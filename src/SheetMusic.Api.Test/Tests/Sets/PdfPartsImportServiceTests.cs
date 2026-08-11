using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Sets;
using SheetMusic.Api.Sets.Services;
using Xunit;

namespace SheetMusic.Api.Test.Tests.Sets;

public sealed class PdfPartsImportServiceTests
{
    [Fact]
    public async Task ImportAsync_ShouldReuseLearnedAlias_WhenDetectedNameIsRepeated()
    {
        var options = new DbContextOptionsBuilder<SheetMusicContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new SheetMusicContext(options);
        var part = new MusicPart { Id = Guid.NewGuid(), Name = "Flute 1", Aliases = [] };
        var set = new SheetMusicSet(1, "Test Set");
        db.MusicParts.Add(part);
        db.SheetMusicSets.Add(set);
        await db.SaveChangesAsync();

        var chatClient = new Mock<IChatClient>();
        chatClient.Setup(client => client.GetResponseAsync(It.IsAny<System.Collections.Generic.IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"match\":\"Flute 1\"}")));
        var blobClient = new Mock<IBlobClient>();
        blobClient.Setup(client => client.AddMusicPartContentAsync(It.IsAny<PartRelatedToSet>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var service = new PdfPartsImportService(db, blobClient.Object, new SheetMusicAgent(chatClient.Object, NullLogger<SheetMusicAgent>.Instance));
        var split = new PdfPartSplitResult([
            new PdfPartGroup("Flute one OCR", 1, 1, 1, "Test") { Content = [1] },
            new PdfPartGroup("Flute one OCR", 2, 2, 1, "Test") { Content = [2] },
        ], []);

        await service.ImportAsync(set, split, CancellationToken.None);

        db.MusicPartAliases.Should().ContainSingle(alias => alias.MusicPartId == part.Id && alias.Alias == "Flute one OCR");
        db.SheetMusicParts.Should().ContainSingle(setPart => setPart.SetId == set.Id && setPart.MusicPartId == part.Id);
        chatClient.Verify(client => client.GetResponseAsync(It.IsAny<System.Collections.Generic.IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        blobClient.Verify(client => client.AddMusicPartContentAsync(It.IsAny<PartRelatedToSet>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}