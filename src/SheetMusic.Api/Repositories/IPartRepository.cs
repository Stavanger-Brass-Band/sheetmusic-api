using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    public interface IPartRepository
    {
        Task<MusicPart> GetOrCreatePartAsync(string partName);
        Task<MusicPart?> GetPartAsync(string partName, bool throwOnMissing = false);
        Task<List<SheetMusicPart>> GetMusicPartsForSetAsync(Guid setId);
        Task<MusicPart> ResolvePartAsync(string partIdentifier);
    }
}