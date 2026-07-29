using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Users;

/// <summary>
/// Exercises the real JWT bearer pipeline (rather than the integration test authentication scheme)
/// to verify that roles are resolved from the database on every request and that role claims carried
/// by the token itself are never trusted.
/// </summary>
public class JwtRoleClaimTests
{
    private const string TestSigningKey = "sheetmusic-api-test-signing-key-not-used-in-production";

    /// <summary>
    /// Undoes the shared factory's <c>ForwardAuthenticate</c> override so requests actually run through
    /// <c>JwtBearerEvents.OnTokenValidated</c>.
    /// </summary>
    private static WebApplicationFactory<Program> CreateJwtFactory()
    {
        var factory = new SheetMusicWebAppFactory();
        return factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
                services.PostConfigureAll<JwtBearerOptions>(o => o.ForwardAuthenticate = null)));
    }

    private static string CreateToken(Guid userId, params Claim[] additionalClaims)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestSigningKey);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim(ClaimTypes.Name, userId.ToString()), .. additionalClaims]),
            Expires = DateTime.UtcNow.AddMinutes(15),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");
        return client;
    }

    [Fact]
    public async Task Request_ShouldGrantRolesFromDatabase_WhenTokenCarriesNoRoles()
    {
        using var factory = CreateJwtFactory();
        var client = CreateClient(factory, CreateToken(TestUser.Noteansvarlig.Identifier));

        var response = await client.GetAsync("sheetmusic/sets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ShouldIgnoreRoleClaimsInToken_AndUseDatabaseRolesInstead()
    {
        using var factory = CreateJwtFactory();

        // Testesen is a Musikant in the database. A token minted with an Admin role claim must not
        // be able to reach an Admin-only endpoint - the database is the only source of truth.
        var client = CreateClient(factory, CreateToken(TestUser.Testesen.Identifier, new Claim(ClaimTypes.Role, "Admin")));

        var forbidden = await client.GetAsync("users");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The token itself is still valid - the 403 above is an authorization decision, not a rejected token.
        var allowed = await client.GetAsync("sheetmusic/sets");
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
