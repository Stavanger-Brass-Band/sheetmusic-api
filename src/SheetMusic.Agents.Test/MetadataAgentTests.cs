using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SheetMusic.Agents;
using Xunit;

namespace SheetMusic.Agents.Test;

public sealed class MetadataAgentTests
{
    [Fact]
    public async Task ClassifyPartAsync_ReturnsNullForUnknownModelMatch()
    {
        var agent = CreateAgent("{\"match\":\"invented\"}");

        var result = await agent.ClassifyPartAsync(
            new PartClassificationRequest("alto.pdf", ["Soprano", "Alto"]),
            CancellationToken.None);

        result.Match.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyCategoryAsync_ReturnsOnlyTwoKnownCategories()
    {
        var agent = CreateAgent("{\"categories\":[\"Concert\",\"March\",\"Invented\"]}");

        var result = await agent.ClassifyCategoryAsync(
            new CategoryClassificationRequest(
                "Title",
                null,
                null,
                [],
                ["Concert", "March", "Funeral"],
                []),
            CancellationToken.None);

        result.Categories.Should().Equal("Concert", "March");
    }

    [Fact]
    public async Task ClassifyPartAsync_ReturnsNullWhenModelFails()
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(chatClient => chatClient.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider failure"));
        var agent = new MetadataAgent(client.Object, NullLoggerFactory.Instance, new ServiceCollection().BuildServiceProvider());

        var result = await agent.ClassifyPartAsync(
            new PartClassificationRequest("alto.pdf", ["Alto"]),
            CancellationToken.None);

        result.Match.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyPartAsync_ReturnsNullForNullCandidates()
    {
        var agent = CreateAgent("{\"match\":\"Alto\"}");

        var result = await agent.ClassifyPartAsync(
            new PartClassificationRequest("alto.pdf", null!),
            CancellationToken.None);

        result.Match.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyPartAsync_AbstainsBeforeModelCallForOversizedCandidates()
    {
        var client = new Mock<IChatClient>();
        var agent = new MetadataAgent(client.Object, NullLoggerFactory.Instance, new ServiceCollection().BuildServiceProvider());

        var result = await agent.ClassifyPartAsync(
            new PartClassificationRequest("alto.pdf", Enumerable.Range(1, 501).Select(index => $"Part {index}").ToArray()),
            CancellationToken.None);

        result.Match.Should().BeNull();
        client.Verify(chatClient => chatClient.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static MetadataAgent CreateAgent(string response)
    {
        var client = new Mock<IChatClient>();
        client
            .Setup(chatClient => chatClient.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        return new MetadataAgent(client.Object, NullLoggerFactory.Instance, new ServiceCollection().BuildServiceProvider());
    }
}
