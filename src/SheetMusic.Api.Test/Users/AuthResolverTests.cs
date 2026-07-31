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
/// Covers <see cref="AuthResolver"/>'s resolution of an <see cref="ApplicationUser"/> and its roles,
/// which are turned into role claims during authentication and drive the <see cref="AuthPolicy"/> policies.
/// </summary>
public class AuthResolverTests : IDisposable
{
    private readonly ServiceProvider serviceProvider;
    private readonly IServiceScope scope;

    public AuthResolverTests()
    {
        var services = new ServiceCollection();

        services.AddDbContext<SheetMusicContext>(options =>
            options.UseInMemoryDatabase($"AuthResolverTests_{Guid.NewGuid()}"));

        services.AddLogging();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SheetMusicContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<AuthResolver>();

        serviceProvider = services.BuildServiceProvider();
        scope = serviceProvider.CreateScope();

        foreach (var roleName in Roles.All)
            RoleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName }).GetAwaiter().GetResult();
    }

    private UserManager<ApplicationUser> UserManager => scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    private RoleManager<IdentityRole<Guid>> RoleManager => scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    private AuthResolver Resolver => scope.ServiceProvider.GetRequiredService<AuthResolver>();

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

    // --- Identity resolution ---

    [Fact]
    public async Task ResolveAsync_ShouldResolveApplicationUser_WhenIdIsApplicationUserId()
    {
        var appUser = await CreateApplicationUserAsync(Roles.Admin);

        var resolved = await Resolver.ResolveAsync(appUser.Id);

        resolved.Should().NotBeNull();
        resolved!.User.Id.Should().Be(appUser.Id);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNull_WhenIdMatchesNothing()
    {
        var resolved = await Resolver.ResolveAsync(Guid.NewGuid());

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnNull_WhenIdMatchesOnlyAMusicianRecord()
    {
        // Regression test for issue #273: Musician is now purely a part-assignment join entity and
        // must never be resolvable as an identity, even if its Id happens to match no ApplicationUser.
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
        var musician = new Musician { Id = Guid.NewGuid(), Name = "Unlinked Musician" };
        db.Musicians.Add(musician);
        await db.SaveChangesAsync();

        var resolved = await Resolver.ResolveAsync(musician.Id);

        resolved.Should().BeNull();
    }

    [Fact]
    public async Task IsInactive_ShouldReflectApplicationUserInactiveFlag()
    {
        var activeUser = await CreateApplicationUserAsync(Roles.Admin, inactive: false);
        var inactiveUser = await CreateApplicationUserAsync(Roles.Admin, inactive: true);

        (await Resolver.ResolveAsync(activeUser.Id))!.IsInactive.Should().BeFalse();
        (await Resolver.ResolveAsync(inactiveUser.Id))!.IsInactive.Should().BeTrue();
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
}
