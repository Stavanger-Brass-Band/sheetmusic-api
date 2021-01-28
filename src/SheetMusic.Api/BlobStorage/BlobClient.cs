using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.Errors;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SheetMusic.Api.BlobStorage
{
    public class BlobClient : IBlobClient
    {
        private const string ContainerName = "sheet-music";

        private readonly IConfiguration configuration;
        private readonly ILogger<BlobClient> logger;

        public BlobClient(IConfiguration configuration, ILogger<BlobClient> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        private CloudBlobContainer GetContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(configuration.GetConnectionString("AzureStorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(ContainerName);

            return container;
        }

        public async Task EnsureContainerExistsAsync()
        {
            var container = GetContainer();
            await container.CreateIfNotExistsAsync();
        }

        public async Task<byte[]> GetMusicPartContentAsync(MusicPartIdentifier identifier)
        {
            var blob = GetBlob(identifier);

            using (var memoryStream = new MemoryStream())
            {
                await blob.DownloadToStreamAsync(memoryStream);
                await memoryStream.FlushAsync();
                return memoryStream.ToArray();
            }
        }

        public async Task<Stream> GetMusicPartContentStreamAsync(MusicPartIdentifier identifier)
        {
            var blob = GetBlob(identifier);

            return await blob.OpenReadAsync();
        }

        public async Task AddMusicPartContentAsync(MusicPartIdentifier identifier, Stream contentStream)
        {
            try
            {
                var blob = GetBlob(identifier);
                await blob.UploadFromStreamAsync(contentStream);
            }
            catch (Exception ex)
            {
                throw new BlobInteractionError("Error occured when uploading from stream", ex);
            }
        }

        private CloudBlockBlob GetBlob(MusicPartIdentifier identifier)
        {
            var container = GetContainer();
            return container.GetBlockBlobReference(identifier.BlobPath);
        }

        public async Task DeleteSetContentAsync(Guid id)
        {
            var container = GetContainer();
            var parts = await container.GetDirectoryReference(id.ToString()).ListBlobsSegmentedAsync(new BlobContinuationToken());

            foreach (var part in parts.Results)
            {
                var blobPart = part as CloudBlockBlob;
                blobPart?.DeleteIfExistsAsync();
            }
        }

        public async Task<bool> HasPdfFileAsync(MusicPartIdentifier identifier)
        {
            var blob = GetBlob(identifier);

            return await blob.ExistsAsync() && blob.Properties.Length > 0;
        }

        public async Task DeletePartContentAsync(MusicPartIdentifier identifier)
        {
            var blob = GetBlob(identifier);
            await blob.DeleteIfExistsAsync();
        }
    }
}
