using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SheetMusic.Api.Sets.Services;
using Xunit;

namespace SheetMusic.Api.Test.Tests.Sets;

public sealed class SheetMusicAgentTests
{
    [Fact]
    public async Task ClassifyPartAsync_ReturnsNull_WhenModelReturnsUnknownMatch()
    {
        var chatClient = CreateChatClient("{\"match\":\"Invented\"}");
        var agent = new SheetMusicAgent(chatClient.Object, NullLogger<SheetMusicAgent>.Instance);

        var result = await agent.ClassifyPartAsync("alto.pdf", ["Alto"], CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ClassifyPartAsync_ReturnsNullWithoutCallingModel_WhenCandidatesAreOversized()
    {
        var chatClient = new Mock<IChatClient>();
        var agent = new SheetMusicAgent(chatClient.Object, NullLogger<SheetMusicAgent>.Instance);

        var result = await agent.ClassifyPartAsync("alto.pdf", Enumerable.Range(1, 501).Select(index => $"Part {index}").ToArray(), CancellationToken.None);

        result.Should().BeNull();
        chatClient.Verify(client => client.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IChatClient> CreateChatClient(string response)
    {
        var client = new Mock<IChatClient>();
        client.Setup(chatClient => chatClient.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        return client;
    }
}