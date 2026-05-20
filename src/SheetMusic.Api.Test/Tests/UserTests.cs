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
    [Fact]
    public async Task GetUser_AsMe_ShouldGiveForbidden_WhenNonAdministrator()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.GetAsync($"users/me");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("bullshit")]
    [InlineData("almost-a-guid-9FA3890E-D008-4791-B841-A1AD283BE86F")]
    public async Task GetUser_WithInvalidIdentifier_ShouldGiveBadRequest(string identifier)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);
        var response = await client.GetAsync($"users/{identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetToken_WithMatchingUserPassword_ShouldBeSuccesfull()
    {
        var client = factory.CreateClient();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new KeyValuePair<string?, string?>("grant_type", "basic"),
            new KeyValuePair<string?, string?>("username", TestUser.Testesen.Email),
            new KeyValuePair<string?, string?>("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync($"token", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetToken_WithWrongPassword_ShouldReturnBadRequest()
    {
        var client = factory.CreateClient();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new KeyValuePair<string?, string?>("grant_type", "basic"),
            new KeyValuePair<string?, string?>("username", TestUser.Testesen.Email),
            new KeyValuePair<string?, string?>("password", "wrong-password")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetToken_WithNonExistentUser_ShouldReturnBadRequest()
    {
        var client = factory.CreateClient();

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new KeyValuePair<string?, string?>("grant_type", "basic"),
            new KeyValuePair<string?, string?>("username", "nonexistent@user.com"),
            new KeyValuePair<string?, string?>("password", "anyPassword")
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync("token", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllUsers_ShouldBeSuccessful_WhenAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllUsers_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("users");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUser_AsMe_ShouldBeSuccessful_WhenAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync("users/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUser_ById_ShouldBeSuccessful_WhenAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await client.GetAsync($"users/{TestUser.Administrator.Identifier}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterUser_ShouldCreateUser_WhenAnonymous()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("users/register", new
        {
            Id = Guid.NewGuid(),
            Name = "New User",
            Email = $"new-{Guid.NewGuid():N}@user.com",
            Password = "SecurePassword123!"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
