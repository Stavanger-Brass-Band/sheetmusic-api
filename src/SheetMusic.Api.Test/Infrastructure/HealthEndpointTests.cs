using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Test.Infrastructure;
using System.Linq;
using System.Net;
using Xunit;

namespace SheetMusic.Api.Test.Infrastructure;

public sealed class HealthEndpointTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public void HealthEndpoint_IsMappedOnce_WhenApiHostStartsInDevelopment()
    {
        var healthEndpoints = factory.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText == "/health");

        healthEndpoints.Should().ContainSingle();
    }

    [Fact]
    public async Task AlivenessEndpoint_ReturnsSuccess_WhenApiHostStartsInProduction()
    {
        using var productionFactory = factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Production));
        using var client = productionFactory.CreateClient();

        var alivenessResponse = await client.GetAsync("/alive");
        var healthResponse = await client.GetAsync("/health");

        alivenessResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}