using System;
using System.IO;
using System.Threading.Tasks;

namespace SheetMusic.Api.BlobStorage
{
    public interface IBlobClient
    {
        Task AddMusicPartContentAsync(PartRelatedToSet identifier, Stream contentStream);
        Task EnsureContainerExistsAsync();
        Task<byte[]> GetMusicPartContentAsync(PartRelatedToSet identifier);
        Task<Stream> GetMusicPartContentStreamAsync(PartRelatedToSet identifier);
        Task DeleteSetContentAsync(Guid id);
        Task<bool> HasPdfFileAsync(PartRelatedToSet identifier);
        Task DeletePartContentAsync(PartRelatedToSet identifier);
    }
}