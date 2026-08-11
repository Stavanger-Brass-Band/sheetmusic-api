using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
public class BlobClient(BlobServiceClient blobServiceClient) : IBlobClient
{
    private const string ContainerName = "sheet-music";
    private readonly SemaphoreSlim containerInitializationLock = new(1, 1);
    private bool containerInitialized;

    private BlobContainerClient GetContainer()
    {
        return blobServiceClient.GetBlobContainerClient(ContainerName);
    }

    public async Task EnsureContainerExistsAsync()
    {
        await EnsureContainerExistsAsync(CancellationToken.None);
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
        using var bufferedContent = contentStream.CanSeek ? null : new MemoryStream();
        Stream uploadContent = contentStream;
        try
        {
            if (bufferedContent is not null)
            {
                await contentStream.CopyToAsync(bufferedContent, cancellationToken);
                bufferedContent.Position = 0;
                uploadContent = bufferedContent;
            }

            var initialPosition = uploadContent.Position;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await EnsureContainerExistsAsync(cancellationToken);
                    await UploadAsync(identifier, uploadContent, cancellationToken);
                    return;
                }
                catch (RequestFailedException error) when (error.Status == 404 && attempt == 0)
                {
                    // Azurite can be recreated while the local API process remains alive, invalidating the cached container state.
                    Volatile.Write(ref containerInitialized, false);
                    uploadContent.Position = initialPosition;
                }
            }
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

    private Task UploadAsync(PartRelatedToSet identifier, Stream contentStream, CancellationToken cancellationToken) =>
        GetBlob(identifier).UploadAsync(contentStream, overwrite: true, cancellationToken: cancellationToken);

    private async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref containerInitialized))
            return;

        await containerInitializationLock.WaitAsync(cancellationToken);
        try
        {
            if (!Volatile.Read(ref containerInitialized))
            {
                await GetContainer().CreateIfNotExistsAsync(cancellationToken: cancellationToken);
                Volatile.Write(ref containerInitialized, true);
            }
        }
        finally
        {
            containerInitializationLock.Release();
        }
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

