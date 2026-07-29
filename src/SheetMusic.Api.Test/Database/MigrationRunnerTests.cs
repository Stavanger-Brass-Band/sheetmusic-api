using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Database;

/// <summary>
/// Covers <see cref="MigrationRunner"/>: startup migrations must be opt-in, not opt-out (issue #238), so
/// an environment that never sets <c>SkipMigrations</c> gets the safe default (skip) instead of silently
/// migrating whatever database it happens to be pointed at.
/// </summary>
public class MigrationRunnerTests
{
    [Fact]
    public void ShouldRunOnStartup_ReturnsFalse_WhenSkipMigrationsIsAbsent()
    {
        var configuration = new ConfigurationBuilder().Build();

        MigrationRunner.ShouldRunOnStartup(configuration).Should().BeFalse();
    }

    [Fact]
    public void ShouldRunOnStartup_ReturnsTrue_WhenSkipMigrationsIsExplicitlyFalse()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [MigrationRunner.SkipMigrationsKey] = "false" })
            .Build();

        MigrationRunner.ShouldRunOnStartup(configuration).Should().BeTrue();
    }

    [Fact]
    public void ShouldRunOnStartup_ReturnsFalse_WhenSkipMigrationsIsExplicitlyTrue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [MigrationRunner.SkipMigrationsKey] = "true" })
            .Build();

        MigrationRunner.ShouldRunOnStartup(configuration).Should().BeFalse();
    }

    [Fact]
    public async Task RunAsAppEntryPointAsync_ReturnsNonZero_WhenMigrationFails()
    {
        // The InMemory provider doesn't support migrations at all, so Database.MigrateAsync() always
        // throws here - a convenient, real failure path to prove the entry point reports it correctly
        // rather than needing a real SQL Server to break against.
        var services = new ServiceCollection();
        services.AddDbContext<SheetMusicContext>(options =>
            options.UseInMemoryDatabase(nameof(RunAsAppEntryPointAsync_ReturnsNonZero_WhenMigrationFails)));
        using var provider = services.BuildServiceProvider();

        var exitCode = await MigrationRunner.RunAsAppEntryPointAsync(provider);

        exitCode.Should().NotBe(0);
    }
}
