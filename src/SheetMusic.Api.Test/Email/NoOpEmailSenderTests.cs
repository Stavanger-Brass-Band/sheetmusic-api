using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Email;
using System.Collections.Generic;
using Xunit;

namespace SheetMusic.Api.Test.Email;

/// <summary>
/// Covers <see cref="IServiceCollectionExtensions.AddSheetMusicEmailSender"/>: a missing
/// <c>Resend:ApiKey</c> must resolve a no-op sender rather than binding <see cref="ResendEmailSender"/>
/// against an empty token, so a test environment can never send real email through a live key
/// configured there by mistake (issue #242).
/// </summary>
public class NoOpEmailSenderTests
{
    [Fact]
    public void AddSheetMusicEmailSender_RegistersNoOpEmailSender_WhenResendApiKeyIsMissing()
    {
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        services.AddSheetMusicEmailSender(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<NoOpEmailSender>();
    }

    [Fact]
    public void AddSheetMusicEmailSender_RegistersResendEmailSender_WhenResendApiKeyIsConfigured()
    {
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ConfigKeys.ResendApiKey] = "test-resend-key" })
            .Build();

        // ResendEmailSender resolves IConfiguration itself (for the from-address/base-url settings), so
        // it must be registered here just as builder.Configuration is in the real Program.cs composition.
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSheetMusicEmailSender(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<ResendEmailSender>();
    }

    [Fact]
    public async System.Threading.Tasks.Task NoOpEmailSender_CompletesSuccessfully_WithoutThrowing()
    {
        var sender = new NoOpEmailSender(new LoggerFactory().CreateLogger<NoOpEmailSender>());

        var act = () => sender.SendPasswordResetAsync("member@example.com", "Test Member", "reset-token");

        await act.Should().NotThrowAsync();
    }
}
