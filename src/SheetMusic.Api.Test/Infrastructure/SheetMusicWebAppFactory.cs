using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Search;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Utility;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace SheetMusic.Api.Test.Infrastructure;

public class SheetMusicWebAppFactory : WebApplicationFactory<Startup>
{
    public ServiceProvider TestServices = null!;
    public Mock<IBlobClient> BlobMock = null!;
    public FakeIndexAdminService FakeIndexAdmin = null!;
    private readonly Guid sessionId;

    public SheetMusicWebAppFactory()
    {
        sessionId = Guid.NewGuid();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SkipMigrations", "true");

        builder.ConfigureTestServices(services =>
        {
            BlobMock = new Mock<IBlobClient>();
            BlobMock.Setup(b => b.GetMusicPartContentAsync(It.IsAny<PartRelatedToSet>()))
                .ReturnsAsync(Array.Empty<byte>());
            BlobMock.Setup(b => b.GetMusicPartContentStreamAsync(It.IsAny<PartRelatedToSet>()))
                .ReturnsAsync(new MemoryStream());
            BlobMock.Setup(b => b.HasPdfFileAsync(It.IsAny<PartRelatedToSet>()))
                .ReturnsAsync(true);
            services.TryRemoveService<IBlobClient>();
            services.AddSingleton(BlobMock.Object);

            FakeIndexAdmin = new FakeIndexAdminService();
            services.TryRemoveService<IIndexAdminService>();
            services.AddSingleton<IIndexAdminService>(FakeIndexAdmin);

            var authBuilder = services.AddAuthentication();
            authBuilder.AddScheme<IntgTestSchemeOptions, IntgTestAuthenticationHandler>(IntgTestAuthenticationHandler.AuthenticationScheme, opts => { });
            services.PostConfigureAll<JwtBearerOptions>(o => o.ForwardAuthenticate = IntgTestAuthenticationHandler.AuthenticationScheme);

            // Remove all EF Core and DbContext-related services
            var efDescriptors = services.Where(d => 
                d.ServiceType.Namespace != null && 
                (d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore") ||
                 d.ServiceType == typeof(SheetMusicContext) ||
                 d.ServiceType == typeof(DbContextOptions<SheetMusicContext>) ||
                 d.ServiceType == typeof(DbContextOptions))).ToList();
            
            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }
            
            // Add InMemory DbContext with fresh services
            services.AddDbContext<SheetMusicContext>(options => 
                options.UseInMemoryDatabase($"SheetMusicIntegrationTest_{sessionId}"));

            var sp = services.BuildServiceProvider();
            TestServices = sp;

            SeedUserData();
        });

        base.ConfigureWebHost(builder);
    }

    private void SeedUserData()
    {
        using var scope = TestServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();

        roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Admin" }).GetAwaiter().GetResult();
        roleManager.CreateAsync(new IdentityRole<Guid> { Name = "Reader" }).GetAwaiter().GetResult();

        // Seed legacy UserGroups for backward compatibility
        var adminGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Admin" };
        var readerGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Reader" };
        db.UserGroups.Add(adminGroup);
        db.UserGroups.Add(readerGroup);

        SeedUser(userManager, db, TestUser.Testesen, "Reader", readerGroup.Id);
        SeedUser(userManager, db, TestUser.Administrator, "Admin", adminGroup.Id);

        db.SaveChanges();
    }

    private static void SeedUser(UserManager<ApplicationUser> userManager, SheetMusicContext db, TestUser testUser, string role, Guid userGroupId)
    {
        var appUser = testUser.AsApplicationUser();
        userManager.CreateAsync(appUser, testUser.Password).GetAwaiter().GetResult();
        userManager.AddToRoleAsync(appUser, role).GetAwaiter().GetResult();

        // Seed legacy HMAC password hash for v1 compatibility
        using var hmac = new System.Security.Cryptography.HMACSHA512();
        var passwordSalt = hmac.Key;
        var passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(testUser.Password));

#pragma warning disable CS0612 // Type or member is obsolete
        db.Musicians.Add(new Musician
        {
            Id = testUser.Identifier,
            Name = testUser.Name,
            Email = testUser.Email,
            Inactive = false,
            UserGroupId = userGroupId,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            ApplicationUserId = appUser.Id
        });
#pragma warning restore CS0612
    }

    public HttpClient CreateClientWithTestToken(TestUser user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", AuthTokenUtilities.WrapAuthToken(user));

        return client;
    }
}
