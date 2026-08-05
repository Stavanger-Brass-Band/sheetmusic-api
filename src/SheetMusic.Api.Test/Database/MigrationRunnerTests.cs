using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using SheetMusic.Api.Migrations;
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
    public void Up_ShouldSeedArkivleserRole_WhenRoleDoesNotExist()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        new TestableAddArkivleserRole().ApplyUp(migrationBuilder);

        var seedOperation = migrationBuilder.Operations.Should().ContainSingle().Which
            .Should().BeOfType<SqlOperation>().Subject;
        seedOperation.Sql.Should().Contain("IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = 'ARKIVLESER')")
            .And.Contain("VALUES ('7a596bb6-bb75-41e1-a299-776990db4d76', 'Arkivleser', 'ARKIVLESER', CONVERT(nvarchar(36), NEWID()))");
    }

    [Fact]
    public void Down_ShouldPreserveArkivleserRole_WhenReverting()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        new TestableAddArkivleserRole().ApplyDown(migrationBuilder);

        migrationBuilder.Operations.Should().BeEmpty();
    }

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

    private sealed class TestableAddArkivleserRole : AddArkivleserRole
    {
        public void ApplyUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);

        public void ApplyDown(MigrationBuilder migrationBuilder) => Down(migrationBuilder);
    }
}
