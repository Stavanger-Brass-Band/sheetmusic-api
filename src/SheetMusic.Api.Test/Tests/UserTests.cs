using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
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
        //var body = await response.Content.ReadAsStringAsync();
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
        var client = factory.CreateClient(); //use vanilla anonymous client

        var collection = new List<KeyValuePair<string?, string?>>
        {
            new KeyValuePair<string?, string?>("grant_type", "basic"),
            new KeyValuePair<string?, string?>("username", TestUser.Testesen.Email),
            new KeyValuePair<string?, string?>("password", TestUser.Testesen.Password)
        };

        var content = new FormUrlEncodedContent(collection);
        var response = await client.PostAsync($"token", content);

        //var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
