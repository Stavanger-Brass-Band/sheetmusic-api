using Azure.Provisioning.AppContainers;
using Azure.Provisioning.ContainerRegistry;
using Azure.Provisioning.OperationalInsights;
using Azure.Provisioning.Search;

var builder = DistributedApplication.CreateBuilder(args);

// Azure SQL once published; a local SQL Server container - with the same persistent lifetime, data
// volume and host port as before - for `aspire run`. Publishing `AddSqlServer` as-is would deploy a SQL
// Server container into Container Apps instead of using Azure SQL (issue #240).
var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer(container => container
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithHostPort(32001));

// One shared logical SQL server, two databases (issue #246): test and production now deploy from a
// single AppHost graph/resource group instead of two independent `aspire deploy --environment`
// invocations, so genuinely shared resources (this server, Search, the ACA environment, the registry,
// Log Analytics) no longer need hand-created "existing" workarounds - each one is simply provisioned
// once. Test keeps its own database so its data never touches production's.
var db = sql.AddDatabase("SheetMusicContext");
var testDb = sql.AddDatabase("SheetMusicContextTest");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume());

// Named "blobs", not "AzureStorageConnectionString": once published this resolves to a service endpoint
// URI plus managed identity rather than a connection string, and the old name becomes actively
// misleading (see BlobStorage/BlobClient.cs and issue #249). Coordinate any further rename with that
// issue - the two are two halves of one change. Shared by both test and prod for now; splitting into
// per-environment containers is a possible future refinement, not required by issue #246.
var blobs = storage.AddBlobs("blobs");

var resendApiKey = builder.AddParameter("resend-api-key", secret: true);
var emailFromAddress = builder.AddParameter("email-from-address");

// Per app, not shared (issue #246): test and prod are different frontend deployments at different
// URLs, so a password-reset/invite email sent by one must never link to the other's frontend.
var emailFrontendBaseUrl = builder.AddParameter("email-frontend-base-url");
var testEmailFrontendBaseUrl = builder.AddParameter("test-email-frontend-base-url");

// JWT signing key. No committed fallback (issue #237): a real value must be supplied per environment,
// including local dev, via `dotnet user-secrets set Parameters:jwt-signing-key <value>` from this
// project. A hardcoded local-dev default was deliberately avoided here - Aspire's publish tooling can
// carry a parameter's default value into the generated deployment manifest, which would risk a
// provisioned environment silently booting with a well-known, forgeable signing key if its own override
// were ever missed. Failing fast with a clear "parameter not set" error is safer than that.
var jwtSigningKey = builder.AddParameter("jwt-signing-key", secret: true);

// Azure AI Search (issue #246): now provisioned by Aspire directly, Free tier. This only works because
// test and prod share one AppHost deploy - Azure allows exactly one Free-tier Search service per
// subscription, so two independent per-environment deploys could never each provision their own.
// Role-based access (Aspire's default - see AddAzureSearchClient in Program.cs) rather than admin keys,
// so there's no search API key to manage as a secret at all.
var search = builder.AddAzureSearch("search")
    .ConfigureInfrastructure(infrastructure =>
    {
        var service = infrastructure.GetProvisionableResources().OfType<SearchService>().Single();
        service.SearchSkuName = SearchServiceSkuName.Free;
    });

// Scale rules (issue #246): test stays at zero idle replicas to get ACA's idle billing rate; production
// keeps one warm replica so real users never hit a cold start. A shared max caps runaway scale-out cost.
// Hardcoded per app below (test vs prod is now a fixed fact about each named resource, not something a
// CI environment variable needs to supply) - still modeled as an Aspire parameter per app so the deploy
// workflow's "scale to zero for this deploy only" step (see deploy.yml) can still override it
// temporarily via `Parameters__<name>`.

// Single shared ACR (Basic SKU, issue #246): genuinely one registry now, since test and prod are one
// deploy/resource group instead of two independent ones - no hand-created "existing" reference needed.
var acr = builder.AddAzureContainerRegistry("acr")
    .ConfigureInfrastructure(infrastructure =>
    {
        var registry = infrastructure.GetProvisionableResources().OfType<ContainerRegistryService>().Single();
        registry.Sku = new ContainerRegistrySku { Name = ContainerRegistrySkuName.Basic };
    });

// Log Analytics daily cap (issue #246): ingestion, not compute, is the line item most likely to quietly
// exceed the cost of running the app itself. The default log level is already Warning (keep it) - this
// cap is the backstop if that ever regresses. Shared by both test and prod logs.
var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("logs")
    .ConfigureInfrastructure(infrastructure =>
    {
        var workspace = infrastructure.GetProvisionableResources().OfType<OperationalInsightsWorkspace>().Single();
        workspace.WorkspaceCapping = new OperationalInsightsWorkspaceCapping { DailyQuotaInGB = 1 };
    });

// A container compute environment to publish into (issue #240), shared by test and prod. Deliberately
// not adding the SPA clients here: Aspire publishes frontend resources as container apps, which would
// replace the free Static Web Apps tier with paid compute and remove the single largest saving in issue
// #234 - the clients deploy via their own Static Web Apps workflow instead.
builder.AddAzureContainerAppEnvironment("sheetmusic-aca-env")
    .WithAzureContainerRegistry(acr)
    .WithAzureLogAnalyticsWorkspace(logAnalytics);

// Builds one API container app (issue #246). `searchIndexPrefix` keeps test and prod's index rebuilds
// from clobbering each other (issue #236) on the one shared Search service above. `minReplicas`/
// `maxReplicas` are this app's real steady-state scale (see "Scale rules" above). `frontendBaseUrl` is
// this app's own frontend deployment (test and prod are different URLs - see the parameters above).
// `corsAllowedOrigins` is this app's own set of browser origins - test and prod are served from
// different frontend hosts, so each app declares only the origin(s) it actually needs.
IResourceBuilder<ProjectResource> AddApi(string name, IResourceBuilder<IResourceWithConnectionString> database, string searchIndexPrefix, int minReplicas, int maxReplicas, IResourceBuilder<ParameterResource> frontendBaseUrl, string[] corsAllowedOrigins)
{
    var minReplicasParameter = builder.AddParameter($"{name}-min-replicas", minReplicas.ToString());
    var maxReplicasParameter = builder.AddParameter($"{name}-max-replicas", maxReplicas.ToString());

    var project = builder.AddProject<Projects.SheetMusic_Api>(name)
        // connectionName fixed to "SheetMusicContext" (not the database resource's own name): Program.cs
        // always reads ConnectionStrings:SheetMusicContext, but the test database resource is named
        // "SheetMusicContextTest" - without this override the test app/job would get an env var named
        // ConnectionStrings__SheetMusicContextTest that the app never looks for.
        .WithReference(database, connectionName: "SheetMusicContext")
        .WaitFor(database)
        .WithReference(blobs)
        .WaitFor(storage)
        .WithReference(search)
        .WaitFor(search)
        .WithEnvironment("Resend__ApiKey", resendApiKey)
        .WithEnvironment("Email__FromAddress", emailFromAddress)
        .WithEnvironment("Email__FrontendBaseUrl", frontendBaseUrl)
        .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
        .WithEnvironment("Search__IndexPrefix", searchIndexPrefix)
        // Public ingress (issue #246): without this the Container App is provisioned with internal-only
        // ingress and nothing outside the ACA environment - including the SPA clients - can reach it.
        .WithExternalHttpEndpoints()
        // Aspire's own health check probing (used by the dashboard/`aspire wait`), reusing the API's
        // existing unconditional `/health` mapping (see Program.cs). Distinct from the ACA-level probes
        // configured below, which the platform itself polls.
        .WithHttpHealthCheck("/health")
        .PublishAsAzureContainerApp((infrastructure, app) =>
        {
            // Ingress target port and ACA liveness/readiness probes (issue #246): Aspire's default
            // ingress target port does not reliably line up with the container's listening port, and
            // ACA does not infer probes from `WithHttpHealthCheck` the way local `aspire run`
            // health-checking does - both must be set explicitly here.
            app.Configuration.Ingress.TargetPort = 8080;

            // ACA ingress-level CORS policy, declared here so it's part of the AppHost model and gets
            // reapplied on every deploy - previously this had to be set by hand against the Container
            // App resource (e.g. via the portal or `az containerapp ingress cors enable`), and Aspire's
            // full-resource redeploy silently wiped it back to "no CORS policy" every time, since nothing
            // in the deployment model declared it. Keep this in sync with AddSheetMusicSecurity's
            // "AllowMember" policy (see Configuration/IServiceCollectionExtensions.cs). `corsAllowedOrigins`
            // is this app's own real frontend origin(s) - test and prod intentionally do not share a list.
            var corsPolicy = new ContainerAppCorsPolicy
            {
                AllowedMethods = { "*" },
                AllowedHeaders = { "*" },
                AllowCredentials = true,
            };
            foreach (var origin in corsAllowedOrigins)
            {
                corsPolicy.AllowedOrigins.Add(origin);
            }
            app.Configuration.Ingress.CorsPolicy = corsPolicy;

            var container = app.Template.Containers[0].Value!;
            container.Probes.Add(new ContainerAppProbe
            {
                ProbeType = ContainerAppProbeType.Liveness,
                HttpGet = new ContainerAppHttpRequestInfo
                {
                    Path = "/health",
                    Port = 8080,
                },
            });
            container.Probes.Add(new ContainerAppProbe
            {
                ProbeType = ContainerAppProbeType.Readiness,
                HttpGet = new ContainerAppHttpRequestInfo
                {
                    Path = "/health",
                    Port = 8080,
                },
            });

            app.Template.Scale.MinReplicas = minReplicasParameter.AsProvisioningParameter(infrastructure);
            app.Template.Scale.MaxReplicas = maxReplicasParameter.AsProvisioningParameter(infrastructure);
        });

    // Local development convenience only: run migrations (and seed dev data) on startup so `aspire run`
    // keeps working with no separate manual step. Startup migrations are opt-in by default
    // (MigrationRunner) precisely so this does not happen once published - the ACA migration Job owns
    // that there.
    if (!builder.ExecutionContext.IsPublishMode)
    {
        project.WithEnvironment("SkipMigrations", "false");
    }

    return project;
}

// Builds one migration ACA Job (issue #246): built from the same image as its matching API and invoked
// with `--migrate` (see Program.cs and MigrationRunner) instead of running migrations on every
// application start. Needs the same environment as the API because Program.cs wires up blob storage,
// Search and the JWT secret unconditionally before checking for `--migrate`, so a missing value would
// crash the job before it ever reached the migration itself.
// - WithEndpointsInEnvironment(_ => false): jobs have no ingress, so the project's default http/https
//   endpoints have nowhere to resolve to at publish time - without this, Bicep synthesis crashes with a
//   "key 'http' was not present" error (found while testing this AppHost for #246).
// - WithExplicitStart keeps this from launching automatically under `aspire run`; it only runs when
//   triggered manually (dashboard, `aspire resource start`, or - once published - the deploy workflow's
//   `az containerapp job start`).
// - PublishAsAzureContainerAppJob turns it into a manually-triggered Azure Container App Job on publish,
//   instead of a second long-running service.
void AddMigrationJob(string name, IResourceBuilder<IResourceWithConnectionString> database)
{
    builder.AddProject<Projects.SheetMusic_Api>(name)
        // Same fixed connectionName override as AddApi above, for the same reason.
        .WithReference(database, connectionName: "SheetMusicContext")
        .WaitFor(database)
        .WithReference(blobs)
        .WaitFor(storage)
        .WithReference(search)
        .WaitFor(search)
        .WithEnvironment("Resend__ApiKey", resendApiKey)
        .WithEnvironment("Email__FromAddress", emailFromAddress)
        .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
        .WithEndpointsInEnvironment(_ => false)
        .WithArgs("--migrate")
        .WithExplicitStart()
        .PublishAsAzureContainerAppJob();
}

var api = AddApi("sheetmusic-api", db, searchIndexPrefix: "", minReplicas: 1, maxReplicas: 3, frontendBaseUrl: emailFrontendBaseUrl, corsAllowedOrigins: ["https://medlem.stavanger-brassband.no"]);
AddMigrationJob("sheetmusic-api-migrate", db);

var apiTest = AddApi("sheetmusic-api-test", testDb, searchIndexPrefix: "test", minReplicas: 0, maxReplicas: 3, frontendBaseUrl: testEmailFrontendBaseUrl, corsAllowedOrigins: ["https://orange-mud-00eed1803.1.azurestaticapps.net"]);
AddMigrationJob("sheetmusic-api-test-migrate", testDb);

builder.Build().Run();
