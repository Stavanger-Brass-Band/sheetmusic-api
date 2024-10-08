﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
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
using System.Net.Http;
using System.Text;

namespace SheetMusic.Api.Test.Infrastructure;

public class SheetMusicWebAppFactory : WebApplicationFactory<Startup>
{
    public ServiceProvider TestServices = null!;
    public Mock<IBlobClient> BlobMock = null!;
    public Mock<IIndexAdminService> IndexAdminMock = null!;
    private readonly Guid sessionId;

    public SheetMusicWebAppFactory()
    {
        sessionId = Guid.NewGuid();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            BlobMock = new Mock<IBlobClient>();
            services.TryRemoveService<IBlobClient>();
            services.AddSingleton(BlobMock.Object);

            IndexAdminMock = new Mock<IIndexAdminService>();
            services.TryRemoveService<IIndexAdminService>();
            services.AddSingleton(IndexAdminMock.Object);

            services.AddEntityFrameworkInMemoryDatabase();
            var builder = services.AddAuthentication();
            builder.AddScheme<IntgTestSchemeOptions, IntgTestAuthenticationHandler>(IntgTestAuthenticationHandler.AuthenticationScheme, opts => { });
            services.PostConfigureAll<JwtBearerOptions>(o => o.ForwardAuthenticate = IntgTestAuthenticationHandler.AuthenticationScheme);

            services.TryRemoveService<DbContextOptions<SheetMusicContext>>();
            services.AddDbContext<SheetMusicContext>(options => options.UseInMemoryDatabase($"SheetMusicIntegrationTest_{sessionId}"));
            TestServices = services.BuildServiceProvider();

            SeedUserData();
        });

        base.ConfigureWebHost(builder);
    }

    private void SeedUserData()
    {
        using var scope = TestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
        var adminGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Admin" };
        db.UserGroups.Add(adminGroup);

        var memberGroup = new UserGroup { Id = Guid.NewGuid(), Name = "Reader" };
        db.UserGroups.Add(memberGroup);

        string password = "intgTest123";
        byte[] passwordSalt;
        byte[] passwordHash;

        using (var hmac = new System.Security.Cryptography.HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        db.Musicians.Add(new Musician
        {
            Id = TestUser.Testesen.Identifier,
            Email = TestUser.Testesen.Email,
            Name = TestUser.Testesen.Name,
            UserGroupId = memberGroup.Id,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        });

        db.Musicians.Add(new Musician
        {
            Id = TestUser.Administrator.Identifier,
            Email = TestUser.Administrator.Email,
            Name = TestUser.Administrator.Name,
            UserGroupId = adminGroup.Id,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        });

        db.SaveChanges();
    }

    public HttpClient CreateClientWithTestToken(TestUser user)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("Authorization", AuthTokenUtilities.WrapAuthToken(user));

        return client;
    }
}
