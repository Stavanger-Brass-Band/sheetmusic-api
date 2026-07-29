using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Users.Authorization;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Users;

/// <summary>
/// Regression tests for the v1 auth bug where legacy <see cref="Musician"/> records that were
/// never linked to an <see cref="ApplicationUser"/> (i.e. <see cref="Musician.ApplicationUserId"/>
/// is null) caused every authenticated request after token issuance to fail with 401/403, even
/// though the legacy `/token` endpoint itself succeeded.
///
/// Also covers the role names carried on the resolved identity, which are turned into role claims
/// during authentication and drive the <see cref="AuthPolicy"/> policies.
/// </summary>
public class LegacyAuthResolverTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    public LegacyAuthResolverTests()
    {
        var services = new ServiceCollection();

        services.AddDbContext<SheetMusicContext>(options =>
            options.UseInMemoryDatabase($"LegacyAuthResolverTests_{Guid.NewGuid()}"));

        services.AddLogging();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SheetMusicContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<LegacyAuthResolver>();

        serviceProvider = services.BuildServiceProvider();
        scope = serviceProvider.CreateScope();

        foreach (var roleName in Roles.All)
            RoleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName }).GetAwaiter().GetResult();
    }

    private UserManager<ApplicationUser> UserManager => scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    private RoleManager<IdentityRole<Guid>> RoleManager => scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    private SheetMusicContext DbContext => scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
    private LegacyAuthResolver Resolver => scope.ServiceProvider.GetRequiredService<LegacyAuthResolver>();

    public void Dispose()
    {
        scope.Dispose();
        serviceProvider.Dispose();
    }

    private async Task<ApplicationUser> CreateApplicationUserAsync(string role, bool inactive = false)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{Guid.NewGuid():N}@user.com",
            Email = $"{Guid.NewGuid():N}@user.com",
            Inactive = inactive
        };

        await UserManager.CreateAsync(user, "SomePassword123!");
        await UserManager.AddToRoleAsync(user, role);

        return user;
    }

#pragma warning disable CS0612 // Type or member is obsolete
    private async Task<Musician> CreateLegacyMusicianAsync(string groupName, bool inactive = false, Guid? applicationUserId = null)
    {
        var group = new UserGroup { Id = Guid.NewGuid(), Name = groupName };
        DbContext.UserGroups.Add(group);

        var musician = new Musician
        {
            Id = Guid.NewGuid(),
            Name = "Legacy Musician",
            Email = $"{Guid.NewGuid():N}@legacy.com",
            Inactive = inactive,
            UserGroupId = group.Id,
            ApplicationUserId = applicationUserId
        };
        DbContext.Musicians.Add(musician);

        await DbContext.SaveChangesAsync();

        return musician;
    }
#pragma warning restore CS0612

    // --- Identity resolution ---

    [Fact]
    public async Task ResolveAsync_ShouldResolveApplicationUser_WhenIdIsApplicationUserId()
    {
        var appUser = await CreateApplicationUserAsync(Roles.Admin);

        var resolved = await Resolver.ResolveAsync(appUser.Id);

        resolved.Should().NotBeNull();
        resolved!.User.Should().NotBeNull();
        resolved.User!.Id.Should().Be(appUser.Id);
        resolved.LegacyMusician.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveApplicationUser_WhenIdIsLinkedLegacyMusicianId()
    {
        var appUser = await CreateApplicationUserAsync(Roles.Admin);
        var musician = await CreateLegacyMusicianAsync("Admin", applicationUserId: appUser.Id);

        var resolved = await Resolver.ResolveAsync(musician.Id);

        resolved.Should().NotBeNull();
        resolved!.User.Should().NotBeNull();
        resolved.User!.Id.Should().Be(appUser.Id);
        resolved.LegacyMusician.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldResolveLegacyMusician_WhenNeverLinkedToApplicationUser()
    {
        var musician = await CreateLegacyMusicianAsync("Admin");

        var resolved = await Resolver.ResolveAsync(musician.Id);

        resolved.Should().NotBeNull();
        resolved!.User.Should().BeNull();
        resolved.LegacyMusician.Should().NotBeNull();
        resolved.LegacyMusician!.Id.Should().Be(musician.Id);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNull_WhenIdMatchesNothing()
    {
        var resolved = await Resolver.ResolveAsync(Guid.NewGuid());

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task IsInactive_ShouldReflectLegacyMusicianInactiveFlag_WhenUnlinked()
    {
        var activeMusician = await CreateLegacyMusicianAsync("Admin", inactive: false);
        var inactiveMusician = await CreateLegacyMusicianAsync("Admin", inactive: true);

        (await Resolver.ResolveAsync(activeMusician.Id))!.IsInactive.Should().BeFalse();
        (await Resolver.ResolveAsync(inactiveMusician.Id))!.IsInactive.Should().BeTrue();
    }

    // --- Role resolution ---

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Noteansvarlig)]
    [InlineData(Roles.Musikant)]
    public async Task ResolveAsync_ShouldReturnIdentityRoles_ForApplicationUser(string role)
    {
        var appUser = await CreateApplicationUserAsync(role);

        var resolved = await Resolver.ResolveAsync(appUser.Id);

        resolved!.Roles.Should().BeEquivalentTo([role]);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnAllIdentityRoles_WhenApplicationUserHasSeveral()
    {
        var appUser = await CreateApplicationUserAsync(Roles.Musikant);
        await UserManager.AddToRoleAsync(appUser, Roles.Noteansvarlig);

        var resolved = await Resolver.ResolveAsync(appUser.Id);

        resolved!.Roles.Should().BeEquivalentTo([Roles.Musikant, Roles.Noteansvarlig]);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnApplicationUserRoles_ForLinkedLegacyMusician()
    {
        var appUser = await CreateApplicationUserAsync(Roles.Admin);
        var musician = await CreateLegacyMusicianAsync("Reader", applicationUserId: appUser.Id);

        var resolved = await Resolver.ResolveAsync(musician.Id);

        resolved!.Roles.Should().BeEquivalentTo([Roles.Admin]);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnAdmin_ForUnlinkedLegacyMusicianInAdminGroup()
    {
        var musician = await CreateLegacyMusicianAsync("Admin");

        var resolved = await Resolver.ResolveAsync(musician.Id);

        resolved!.Roles.Should().BeEquivalentTo([Roles.Admin]);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnMusikant_ForUnlinkedLegacyMusicianInReaderGroup()
    {
        var musician = await CreateLegacyMusicianAsync("Reader");

        var resolved = await Resolver.ResolveAsync(musician.Id);

        resolved!.Roles.Should().BeEquivalentTo([Roles.Musikant]);
    }
}
