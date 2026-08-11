using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Sets;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.BlobStorage;

/// <summary>
/// Reads and writes sheet music PDFs in blob storage via an injected <see cref="BlobServiceClient"/>.
/// The client is resolved through the Aspire Azure Storage Blobs integration (see Program.cs), which
/// transparently handles both a local emulator connection string and, once published, a service
/// endpoint URI combined with a managed identity credential - so this class never needs to know which
/// case it is running under.
/// </summary>
public class BlobClient(BlobServiceClient blobServiceClient, IConfiguration configuration) : IBlobClient
{
    private readonly string containerName = configuration["BlobStorage:ContainerName"] ?? "sheet-music";

    private BlobContainerClient GetContainer()
    {
        return blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task EnsureContainerExistsAsync()
    {
        var container = GetContainer();
        await container.CreateIfNotExistsAsync();
    }

    public async Task<byte[]> GetMusicPartContentAsync(PartRelatedToSet identifier)
    {
        var blob = GetBlob(identifier);

        using var memoryStream = new MemoryStream();
        await blob.DownloadToAsync(memoryStream);
        await memoryStream.FlushAsync();
        return memoryStream.ToArray();
    }

    public async Task<Stream> GetMusicPartContentStreamAsync(PartRelatedToSet identifier)
    {
        var blob = GetBlob(identifier);

        return await blob.OpenReadAsync();
    }

    public async Task AddMusicPartContentAsync(PartRelatedToSet identifier, Stream contentStream, CancellationToken cancellationToken)
    {
        try
        {
            var blob = GetBlob(identifier);
            await blob.UploadAsync(contentStream, overwrite: true, cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BlobInteractionError("Error occured when uploading from stream", ex);
        }
    }

    private Azure.Storage.Blobs.BlobClient GetBlob(PartRelatedToSet identifier)
    {
        var container = GetContainer();
        return container.GetBlobClient(identifier.BlobPath);
    }

    public async Task DeleteSetContentAsync(Guid id)
    {
        var container = GetContainer();

        await foreach (var blobItem in container.GetBlobsAsync(new GetBlobsOptions { Prefix = $"{id}/" }))
        {
            await container.DeleteBlobIfExistsAsync(blobItem.Name);
        }
    }

    public async Task<bool> HasPdfFileAsync(PartRelatedToSet identifier)
    {
        var blob = GetBlob(identifier);

        if (!await blob.ExistsAsync())
        {
            return false;
        }

        var properties = await blob.GetPropertiesAsync();
        return properties.Value.ContentLength > 0;
    }

    public async Task DeletePartContentAsync(PartRelatedToSet identifier)
    {
        var blob = GetBlob(identifier);
        await blob.DeleteIfExistsAsync();
    }
}

