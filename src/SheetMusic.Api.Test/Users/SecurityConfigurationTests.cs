using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using Xunit;

namespace SheetMusic.Api.Test.Users;

/// <summary>
/// Verifies that a missing <c>Jwt:SigningKey</c> - the JWT signing key - fails application startup
/// fast rather than silently booting on some other fallback. Before issue #237, a value was committed to
/// <c>appsettings.json</c>; anyone missing an environment-specific override would boot successfully on a
/// signing key public in source control, letting anyone forge an admin token.
/// </summary>
public class SecurityConfigurationTests
{
    [Fact]
    public void Startup_Fails_WhenJwtSigningKeyIsMissing()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("SkipMigrations", "true");
            // Deliberately not setting Jwt:SigningKey - appsettings.json no longer provides one.
        });

        Action act = () =>
        {
            using var client = factory.CreateClient();
        };

        act.Should().Throw<Exception>();
    }
}
