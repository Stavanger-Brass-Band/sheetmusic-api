using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Repositories
{
    [Obsolete("Use CQRS pattern instead")]
    public class PartRepository : IPartRepository
    {
        private readonly SheetMusicContext context;

        public PartRepository(SheetMusicContext context)
        {
            this.context = context;
        }

        public async Task<MusicPart> GetOrCreatePartAsync(string partName)
        {
            var part = await GetPartAsync(partName);

            if (part == null)
            {
                part = new MusicPart { Id = Guid.NewGuid(), Name = partName };
                await context.MusicParts.AddAsync(part);
                await context.SaveChangesAsync();
            }

            return part;
        }

        public async Task<MusicPart?> GetPartAsync(string partName, bool throwOnMissing = false)
        {
            var partNameLower = partName.ToLower();

            var query = from mp in context.MusicParts
                        from mpa in mp.Aliases.DefaultIfEmpty()
                        where mp.Name.ToLower() == partNameLower ||
                             (mpa.Alias.ToLower() == partNameLower && mpa.Enabled)
                        select mp;

            var part = await query.Include(p => p.Aliases).FirstOrDefaultAsync();

            if (part == null && throwOnMissing)
            {
                throw new NotFoundError($"parts/{partName}");
            }

            return part;
        }
    }
}
