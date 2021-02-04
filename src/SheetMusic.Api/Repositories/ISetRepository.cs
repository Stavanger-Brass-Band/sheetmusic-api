using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    [Obsolete("Use CQRS pattern instead")]
    public interface ISetRepository
    {
        Task AddPartContentForSetAsync(string identifier, Stream zipFileStream);
        Task<SheetMusicSet> ResolveByIdentiferAsync(string identifier);
        Task<Stream> GetPartPdfsAsZipForSetAsync(string identifier);
        Task<List<SheetMusicSet>> SearchAsync(string searchTerm);
        Task AddMusicPartForSetAsync(Guid setId, Guid partId);
    }
}