using System;
using System.IO;
using System.Threading.Tasks;

namespace SheetMusic.Api.BlobStorage
{
    public interface IBlobClient
    {
        Task AddMusicPartContentAsync(MusicPartIdentifier identifier, Stream contentStream);
        Task EnsureContainerExistsAsync();
        Task<byte[]> GetMusicPartContentAsync(MusicPartIdentifier identifier);
        Task<Stream> GetMusicPartContentStreamAsync(MusicPartIdentifier identifier);
        Task DeleteSetContentAsync(Guid id);
        Task<bool> HasPdfFileAsync(MusicPartIdentifier identifier);
        Task DeletePartContentAsync(MusicPartIdentifier identifier);
    }
}