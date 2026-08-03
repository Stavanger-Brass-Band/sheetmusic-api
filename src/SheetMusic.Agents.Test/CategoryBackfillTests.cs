using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SheetMusic.Agents;
using Xunit;

namespace SheetMusic.Agents.Test;

public sealed class CategoryBackfillTests
{
    [Fact]
    public async Task RunAsync_WritesAiProvenance_WhenSuggestionIsAccepted()
    {
        var options = new DbContextOptionsBuilder<AgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new AgentDbContext(options);
        var category = new AgentCategory { Id = Guid.NewGuid(), Name = "Concert" };
        var set = new AgentSet { Id = Guid.NewGuid(), Title = "Spring Concert" };
        db.Categories.Add(category);
        db.Sets.Add(set);
        await db.SaveChangesAsync();

        var chatClient = new Mock<IChatClient>();
        chatClient
            .Setup(client => client.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"categories\":[\"Concert\"]}")));
        var metadataAgent = new MetadataAgent(
            chatClient.Object,
            NullLoggerFactory.Instance,
            new ServiceCollection().BuildServiceProvider());
        var backfill = new CategoryBackfill(db, metadataAgent, NullLogger<CategoryBackfill>.Instance);

        await backfill.RunAsync(1, false, CancellationToken.None);

        var assignment = await db.SetCategories.SingleAsync();
        assignment.Source.Should().Be("Ai");
        assignment.ModelVersion.Should().Be(CategoryBackfill.ModelVersion);
        assignment.PromptVersion.Should().Be(CategoryBackfill.PromptVersion);
        assignment.SuggestedAt.Should().NotBeNull();
    }
}
