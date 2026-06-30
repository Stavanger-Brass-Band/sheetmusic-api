using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests;

[CollectionDefinition(Collections.User)]
public class UserTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    // --- V1 (legacy HMAC auth, default version) ---

    [Fact]
    public async Task V1_GetToken_WithMatchingUserPassword_ShouldBeSuccessful()
    {
        var client = factory.CreateClient();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V1_GetToken_WithWrongPassword_ShouldReturnBadRequest()
    {
        var client = factory.CreateClient();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", "wrong-password")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V1_GetToken_WithNonExistentUser_ShouldReturnBadRequest()
    {
        var client = factory.CreateClient();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", "nonexistent@user.com"),
            new("password", "anyPassword")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V1_GetAllUsers_ShouldBeSuccessful_WhenAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V1_GetAllUsers_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V1_GetUser_AsMe_ShouldBeSuccessful_WhenAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V1_GetUser_AsMe_ShouldGiveForbidden_WhenNonAdministrator()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("bullshit")]
    [InlineData("almost-a-guid-9FA3890E-D008-4791-B841-A1AD283BE86F")]
    public async Task V1_GetUser_WithInvalidIdentifier_ShouldGiveBadRequest(string identifier)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);
        var response = await client.GetAsync($"users/{identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V1_GetUser_ById_ShouldBeSuccessful_WhenAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync($"users/{TestUser.Administrator.Identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // --- V2 (Identity auth) ---

    private HttpClient CreateV2Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");
        return client;
    }

    private HttpClient CreateV2ClientWithTestToken(TestUser user)
    {
        var client = factory.CreateClientWithTestToken(user);
        client.DefaultRequestHeaders.Add("x-api-version", "2.0");
        return client;
    }

    [Fact]
    public async Task V2_GetToken_WithMatchingUserPassword_ShouldBeSuccessful()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetToken_WithWrongPassword_ShouldReturnBadRequest()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", TestUser.Testesen.Email),
            new("password", "wrong-password")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_WithNonExistentUser_ShouldReturnBadRequest()
    {
        var client = CreateV2Client();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", "nonexistent@user.com"),
            new("password", "anyPassword")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetToken_ShouldReturnBadRequest_WhenUserIsInactive()
    {
        var client = CreateV2Client();
        var email = $"inactive-{Guid.NewGuid():N}@user.com";

        await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Inactive User",
            Email = email,
            Password = "SecurePassword123!"
        });

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new("grant_type", "basic"),
            new("username", email),
            new("password", "SecurePassword123!")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RegisterUser_ShouldCreateUser_WhenAnonymous()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "New User",
            Email = $"new-{Guid.NewGuid():N}@user.com",
            Password = "SecurePassword123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task V2_RegisterUser_ShouldReturnBadRequest_WhenDuplicateEmail()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Duplicate User",
            Email = TestUser.Testesen.Email,
            Password = "SecurePassword123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_RegisterUser_ShouldReturnBadRequest_WhenWeakPassword()
    {
        var client = CreateV2Client();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "Weak Password User",
            Email = $"weak-{Guid.NewGuid():N}@user.com",
            Password = "short"
        });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetAllUsers_ShouldBeSuccessful_WhenAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetAllUsers_ShouldBeForbidden_WhenReader()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task V2_GetUser_AsMe_ShouldBeSuccessful_WhenAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetUser_AsMe_ShouldGiveForbidden_WhenNonAdministrator()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);
        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("bullshit")]
    [InlineData("almost-a-guid-9FA3890E-D008-4791-B841-A1AD283BE86F")]
    public async Task V2_GetUser_WithInvalidIdentifier_ShouldGiveBadRequest(string identifier)
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);
        var response = await client.GetAsync($"users/{identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task V2_GetUser_ById_ShouldBeSuccessful_WhenAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync($"users/{TestUser.Administrator.Identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_GetUser_ById_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync($"users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldBeSuccessful_WhenAdminUpdatesAnother()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Administrator);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Administrator.Identifier}", new
        {
            Password = "UpdatedAdmin123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task V2_UpdateUser_ShouldBeForbidden_WhenNonAdmin()
    {
        var client = CreateV2ClientWithTestToken(TestUser.Testesen);

        var response = await client.PutAsJsonAsync($"users/{TestUser.Administrator.Identifier}", new
        {
            Password = "HackerPassword1!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
