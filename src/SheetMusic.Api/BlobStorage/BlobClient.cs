using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Sets;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SheetMusic.Api.BlobStorage;

public class BlobClient(IConfiguration configuration) : IBlobClient
{
    private const string ContainerName = "sheet-music";

    private BlobContainerClient GetContainer()
    {
        return new BlobContainerClient(configuration.GetConnectionString("AzureStorageConnectionString"), ContainerName);
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

    public async Task AddMusicPartContentAsync(PartRelatedToSet identifier, Stream contentStream)
    {
        try
        {
            var blob = GetBlob(identifier);
            await blob.UploadAsync(contentStream, overwrite: true);
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

