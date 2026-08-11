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

    [Fact]
    public async Task ExtractPartNamesAsync_ReturnsOrderedParts_WhenHeadersContainMetadata()
    {
        var chatClient = CreateChatClient(
            "{\"title\":\"Overture\",\"composer\":\"Composer Name\",\"arranger\":\"Arranged by Name\"}",
            "{\"parts\":[\"Flute 1\",\"Tuba\"]}");
        var agent = new SheetMusicAgent(chatClient.Object, NullLogger<SheetMusicAgent>.Instance);

        var result = await agent.ExtractPartNamesAsync([
            "Overture - Composer Name - Arranged by Name - Flute 1",
            "Overture - Composer Name - Arranged by Name - Tuba",
        ], CancellationToken.None);

        result.Should().Equal("Flute 1", "Tuba");
        chatClient.Verify(client => client.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExtractPartNamesAsync_RemovesWholeSharedMetadata_BeforePartExtraction()
    {
        var requests = new List<string>();
        var chatClient = new Mock<IChatClient>();
        chatClient.Setup(chatClient => chatClient.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> messages, ChatOptions? _, CancellationToken _) =>
            {
                requests.Add(messages.Single().Text);
                var response = requests.Count == 1
                    ? "{\"title\":\"Overture\",\"composer\":\"Composer Name\",\"arranger\":\"A\"}"
                    : "{\"parts\":[\"Flute 1\"]}";
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, response));
            });
        var agent = new SheetMusicAgent(chatClient.Object, NullLogger<SheetMusicAgent>.Instance);

        var result = await agent.ExtractPartNamesAsync(["Overture Composer Name A Flute 1"], CancellationToken.None);

        result.Should().Equal("Flute 1");
        requests[1].Should().NotContain("Overture").And.NotContain("Composer Name").And.Contain("A Flute 1");
    }

    private static Mock<IChatClient> CreateChatClient(params string[] responses)
    {
        var client = new Mock<IChatClient>();
        var sequence = client.SetupSequence(chatClient => chatClient.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()));
        foreach (var response in responses)
            sequence.ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, response)));
        return client;
    }
}