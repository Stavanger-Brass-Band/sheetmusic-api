using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Test.Infrastructure;
using System.Linq;
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
}