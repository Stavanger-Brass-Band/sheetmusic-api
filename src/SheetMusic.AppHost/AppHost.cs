var builder = DistributedApplication.CreateBuilder(args);

// Azure SQL once published; a local SQL Server container - with the same persistent lifetime, data
// volume and host port as before - for `aspire run`. Publishing `AddSqlServer` as-is would deploy a SQL
// Server container into Container Apps instead of using Azure SQL (issue #240).
var sql = builder.AddAzureSqlServer("sql")
    .RunAsContainer(container => container
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithHostPort(32001));

var db = sql.AddDatabase("SheetMusicContext");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume());

// Named "blobs", not "AzureStorageConnectionString": once published this resolves to a service endpoint
// URI plus managed identity rather than a connection string, and the old name becomes actively
// misleading (see BlobStorage/BlobClient.cs and issue #249). Coordinate any further rename with that
// issue - the two are two halves of one change.
var blobs = storage.AddBlobs("blobs");

var resendApiKey = builder.AddParameter("resend-api-key", secret: true);
var emailFromAddress = builder.AddParameter("email-from-address");
var emailFrontendBaseUrl = builder.AddParameter("email-frontend-base-url");

// JWT signing key. No committed fallback (issue #237): a real value must be supplied per environment,
// including local dev, via `dotnet user-secrets set Parameters:app-settings-secret <value>` from this
// project. A hardcoded local-dev default was deliberately avoided here - Aspire's publish tooling can
// carry a parameter's default value into the generated deployment manifest, which would risk a
// provisioned environment silently booting with a well-known, forgeable signing key if its own override
// were ever missed. Failing fast with a clear "parameter not set" error is safer than that.
var appSettingsSecret = builder.AddParameter("app-settings-secret", secret: true);

// Azure AI Search connection. Defaulted to empty so `aspire run` doesn't block on a parameter most local
// dev doesn't need; part search simply stays unavailable locally until these are supplied via user
// secrets, exactly as before this issue (#241) added them to the AppHost.
var searchHost = builder.AddParameter("search-host", "");
var searchAdminKey = builder.AddParameter("search-admin-key", "", secret: true);

// Prefix Azure AI Search index names per environment (issue #236) so test and prod can share the single
// Free-tier search service without one environment's index rebuild deleting the other's. Empty by
// default, preserving the historical unprefixed index name.
var searchIndexPrefix = builder.AddParameter("search-index-prefix", "");

var api = builder.AddProject<Projects.SheetMusic_Api>("sheetmusic-api")
    .WithReference(db)
    .WaitFor(db)
    .WithReference(blobs)
    .WaitFor(storage)
    .WithEnvironment("Resend__ApiKey", resendApiKey)
    .WithEnvironment("Email__FromAddress", emailFromAddress)
    .WithEnvironment("Email__FrontendBaseUrl", emailFrontendBaseUrl)
    .WithEnvironment("AppSettings__Secret", appSettingsSecret)
    .WithEnvironment("Search__Host", searchHost)
    .WithEnvironment("Search__AdminKey", searchAdminKey)
    .WithEnvironment("Search__IndexPrefix", searchIndexPrefix);

// Local development convenience only: run migrations (and seed dev data) on startup so `aspire run`
// keeps working with no separate manual step. Startup migrations are opt-in by default (MigrationRunner)
// precisely so this does not happen once published - the ACA migration Job (issue #246) owns that there.
if (!builder.ExecutionContext.IsPublishMode)
{
    api.WithEnvironment("SkipMigrations", "false");
}

// A container compute environment to publish the API into (issue #240). Deliberately not adding the SPA
// clients here: Aspire publishes frontend resources as container apps, which would replace the free
// Static Web Apps tier with paid compute and remove the single largest saving in issue #234 - the clients
// deploy via their own Static Web Apps workflow instead.
builder.AddAzureContainerAppEnvironment("sheetmusic-aca-env");

builder.Build().Run();
