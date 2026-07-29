using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Database;

/// <summary>
/// Startup migrations are opt-in, not opt-out: a fresh environment that never sets
/// <see cref="SkipMigrationsKey"/> gets the safe behaviour (no migration at boot) rather than silently
/// running schema changes against whatever database it happens to be pointed at. This matters once
/// migrations are applied by a dedicated deploy-time job (see <see cref="RunAsAppEntryPointAsync"/>)
/// instead of on every application start - a missing environment variable must not resurrect the old
/// behaviour in production.
/// </summary>
public static class MigrationRunner
{
    public const string SkipMigrationsKey = "SkipMigrations";

    /// <summary>
    /// Whether the application should apply pending migrations itself on startup. Defaults to
    /// <c>false</c> (skip) when <see cref="SkipMigrationsKey"/> is absent; set it explicitly to
    /// <c>false</c> to opt back in, e.g. for local development via the AppHost.
    /// </summary>
    public static bool ShouldRunOnStartup(IConfiguration configuration) =>
        !(configuration.GetValue<bool?>(SkipMigrationsKey) ?? true);

    /// <summary>
    /// Applies pending migrations and returns a process exit code, for use as the entry point invoked
    /// by a dedicated migration job (e.g. an Azure Container Apps Job built from the same image as the
    /// API) rather than as part of application startup.
    /// </summary>
    public static async Task<int> RunAsAppEntryPointAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();

        try
        {
            await db.Database.MigrateAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Migration failed: {ex}");
            return 1;
        }
    }
}
