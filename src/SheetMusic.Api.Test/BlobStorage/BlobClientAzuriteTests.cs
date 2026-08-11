using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Sets;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.BlobStorage;

/// <summary>
/// Exercises the real <see cref="BlobClient"/> against a real Azurite
/// container, rather than the <see cref="Moq.Mock{IBlobClient}"/> every other integration test uses (see
/// <see cref="SheetMusicWebAppFactory"/>). Without this, a storage-authentication regression - such as
/// the connection-string-vs-URI defect fixed in issue #249 - could pass the whole suite while being
/// fatal in production. See issue #250.
/// </summary>
[Collection(Collections.Azurite)]
public class BlobClientAzuriteTests(AzuriteFixture azurite)
{
    [Fact]
    public async Task RoundTripsContent_WhenBlobServiceClientBuiltFromConnectionString()
    {
        var blobClient = CreateBlobClient(azurite.CreateClientFromConnectionString());
        await AssertRoundTripsContentAsync(blobClient);
    }

    [Fact]
    public async Task RoundTripsContent_WhenBlobServiceClientBuiltFromServiceUriAndCredential()
    {
        var blobClient = CreateBlobClient(azurite.CreateClientFromServiceUri());
        await AssertRoundTripsContentAsync(blobClient);
    }

    [Fact]
    public async Task AddMusicPartContentAsync_ThrowsCancellation_WhenRequestIsCancelled()
    {
        var blobClient = CreateBlobClient(azurite.CreateClientFromConnectionString());
        await blobClient.EnsureContainerExistsAsync();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Func<Task> upload = () => blobClient.AddMusicPartContentAsync(
            new PartRelatedToSet(Guid.NewGuid(), Guid.NewGuid()),
            new MemoryStream(Encoding.UTF8.GetBytes("content")),
            cancellationSource.Token);

        await upload.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task HasPdfFileAsync_ReturnsFalse_WhenContentIsInAnotherConfiguredContainer()
    {
        var blobServiceClient = azurite.CreateClientFromConnectionString();
        var source = CreateBlobClient(blobServiceClient, $"sheet-music-{Guid.NewGuid():N}");
        var isolated = CreateBlobClient(blobServiceClient, $"sheet-music-{Guid.NewGuid():N}");
        var identifier = new PartRelatedToSet(Guid.NewGuid(), Guid.NewGuid());

        await source.EnsureContainerExistsAsync();
        await source.AddMusicPartContentAsync(identifier, new MemoryStream(Encoding.UTF8.GetBytes("content")), CancellationToken.None);

        (await isolated.HasPdfFileAsync(identifier)).Should().BeFalse();
    }

    private static async Task AssertRoundTripsContentAsync(IBlobClient blobClient)
    {
        await blobClient.EnsureContainerExistsAsync();

        var identifier = new PartRelatedToSet(Guid.NewGuid(), Guid.NewGuid());
        var content = Encoding.UTF8.GetBytes("Azurite round-trip content");

        await blobClient.AddMusicPartContentAsync(identifier, new MemoryStream(content), CancellationToken.None);

        (await blobClient.HasPdfFileAsync(identifier)).Should().BeTrue();

        var downloaded = await blobClient.GetMusicPartContentAsync(identifier);
        downloaded.Should().Equal(content);

        await blobClient.DeletePartContentAsync(identifier);

        (await blobClient.HasPdfFileAsync(identifier)).Should().BeFalse();
    }

    private static BlobClient CreateBlobClient(Azure.Storage.Blobs.BlobServiceClient blobServiceClient, string? containerName = null)
    {
        var values = containerName is null
            ? []
            : new Dictionary<string, string?> { ["BlobStorage:ContainerName"] = containerName };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new BlobClient(blobServiceClient, configuration);
    }
}
