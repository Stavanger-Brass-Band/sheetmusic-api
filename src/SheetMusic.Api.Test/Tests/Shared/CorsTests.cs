using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests.Shared;

/// <summary>
/// UseCors must run before UseAuthentication/UseAuthorization (see Program.cs) so that CORS headers are
/// present even on 401/403 responses - otherwise the browser reports a CORS error instead of surfacing
/// the real auth failure to the frontend.
/// </summary>
[Collection(Collections.Set)]
public class CorsTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task GetCategoryList_ShouldIncludeCorsHeader_WhenUnauthenticatedFromAllowedOrigin()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "categories");
        request.Headers.Add("Origin", "https://medlem.stavanger-brassband.no");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().ContainSingle(h => h.Key == "Access-Control-Allow-Origin")
            .Which.Value.Should().ContainSingle("https://medlem.stavanger-brassband.no");
    }

    [Fact]
    public async Task GetCategoryList_ShouldNotIncludeCorsHeader_WhenOriginNotAllowed()
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "categories");
        request.Headers.Add("Origin", "https://not-allowed.example.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.Should().NotContain(h => h.Key == "Access-Control-Allow-Origin");
    }
}
