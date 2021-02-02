using SheetMusic.Api.Database.Entities;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    [Obsolete("Use CQRS pattern instead")]
    public interface IPartRepository
    {
        Task<MusicPart> GetOrCreatePartAsync(string partName);
        Task<MusicPart?> GetPartAsync(string partName, bool throwOnMissing = false);
    }
}