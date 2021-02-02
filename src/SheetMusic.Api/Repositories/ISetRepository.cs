using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    public interface ISetRepository
    {
        Task AddPartContentForSetAsync(string identifier, Stream zipFileStream);
        Task<List<SheetMusicSet>> GetSetsAsync();
        Task<SheetMusicSet> ResolveByIdentiferAsync(string identifier);
        Task<Stream> GetPartPdfsAsZipForSetAsync(string identifier);
        Task<int> GetNextAvailableArchiveNumberAsync();
        Task AddNewSetAsync(SheetMusicSet set);
        Task<List<SheetMusicSet>> SearchAsync(string searchTerm);
        Task<List<SheetMusicSet>> GetSetsWithPartsAsync();
        Task AddMusicPartForSetAsync(Guid setId, Guid partId);
    }
}