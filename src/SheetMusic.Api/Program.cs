using Asp.Versioning.ApiExplorer;
using Azure.Storage.Blobs;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Search;
using SheetMusic.Api.Users;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<SheetMusicContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SheetMusicContext")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<SheetMusicContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(1));

builder.Services.AddSheetMusicEmailSender(builder.Configuration);

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBodyLengthLimit = int.MaxValue;
});

builder.Services.AddSheetMusicSecurity(builder.Configuration);
builder.Services.AddSheetMusicVersioning();
builder.Services.AddSheetMusicOpenApi();
builder.Services.AddSheetMusicRateLimiting(builder.Configuration);

// Resolves the emulator-connection-string case (local Azurite via Aspire's RunAsEmulator) and the
// published service-endpoint-URI-plus-managed-identity case transparently, so BlobClient never has to
// know which one it's running under. See BlobStorage/BlobClient.cs.
builder.AddAzureBlobServiceClient("blobs");
builder.Services.AddSingleton<IBlobClient, SheetMusic.Api.BlobStorage.BlobClient>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IIndexAdminService, IndexAdminService>();

// Persist the Data Protection key ring to blob storage (container "data-protection-keys", blob
// "keys.xml") rather than relying on the local filesystem. App Service auto-persists keys to %HOME%,
// but Azure Container Apps' filesystem is ephemeral: every scale-to-zero cold start would otherwise
// regenerate the ring, invalidating outstanding password-reset tokens and breaking token validation
// across replicas. See BlobStorage/AzureBlobXmlRepository.cs for why a small custom IXmlRepository is
// used instead of Microsoft's official (legacy-SDK-only) DataProtection.AzureStorage package.
//
// Resolving the BlobServiceClient requires a throwaway service provider (same pattern as
// AddSheetMusicOpenApi above). Guarded: hosts with no blob storage configured at all (e.g. the
// WebApplicationFactory-based test host, which doesn't run through the AppHost) fall back to Data
// Protection's default local key storage instead of failing application startup - matching the
// historical, storage-optional behaviour.
builder.Services.AddDataProtection().SetApplicationName("SheetMusic.Api");
try
{
    using var blobServiceProvider = builder.Services.BuildServiceProvider();
    var blobServiceClient = blobServiceProvider.GetRequiredService<BlobServiceClient>();
    var keyRingContainer = blobServiceClient.GetBlobContainerClient("data-protection-keys");

    builder.Services.Configure<KeyManagementOptions>(options =>
        options.XmlRepository = new AzureBlobXmlRepository(keyRingContainer, "keys.xml"));
}
catch (InvalidOperationException)
{
    // No blob storage configured for this host; Data Protection uses its default key storage.
}

builder.Services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<Program>());

builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

// A dedicated migration entry point, invoked as `dotnet SheetMusic.Api.dll --migrate` from a deploy-time
// job built from this same image, rather than running as part of every application start. Exits with a
// non-zero code on failure so the invoking job/pipeline can detect it.
if (args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase)))
{
    var migrationExitCode = await MigrationRunner.RunAsAppEntryPointAsync(app.Services);
    Environment.Exit(migrationExitCode);
}

if (MigrationRunner.ShouldRunOnStartup(builder.Configuration))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DatabaseSeeder.SeedDevelopmentDataAsync(scope.ServiceProvider);
    }
}

app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseForwardedHeaders();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseCors("AllowMember");
app.UseRateLimiter();

var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

app.MapOpenApi();

app.MapScalarApiReference(options =>
{
    options.Title = "SheetMusic API";
    options.AddDocuments(provider.ApiVersionDescriptions.Select(description => description.GroupName));

    // Preselects the oauth2 scheme (password flow against /token, see OAuthSecuritySchemeTransformer) so the
    // Authentication panel opens ready for a user to type their own username/password and try it out interactively.
    // Persisting the resulting token means it survives page reloads instead of being lost every refresh.
    options.AddPreferredSecuritySchemes("oauth2")
        .AddPasswordFlow("oauth2", flow => flow.ClientId = "sheetmusic-api")
        .EnablePersistentAuthentication();
});

app.MapControllers();
app.MapHealthChecks("/health");

app.MapDefaultEndpoints();

app.Run();

/// <summary>
/// Exposes the top-level statement entry point as a public partial class so
/// <c>WebApplicationFactory&lt;Program&gt;</c> can boot this application for integration tests.
/// </summary>
public partial class Program;

