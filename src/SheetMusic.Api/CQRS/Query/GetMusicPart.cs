using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetMusicPart(string partIdentifier) : IRequest<MusicPart?>
{
    public string PartIdentifier { get; } = partIdentifier;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetMusicPart, MusicPart?>
    {
        public async Task<MusicPart?> Handle(GetMusicPart request, CancellationToken cancellationToken)
        {
            MusicPart? result = null;

            if (Guid.TryParse(request.PartIdentifier, out var guid))
            {
                result = await db.MusicParts
                    .Include(p => p.Aliases)
                    .FirstOrDefaultAsync(p => p.Id == guid, cancellationToken: cancellationToken);
            }
            else
            {
                var partNameLower = request.PartIdentifier.ToLower();

                var query = from mp in db.MusicParts
                            from mpa in mp.Aliases.DefaultIfEmpty()
                            where mp.Name.ToLower() == partNameLower ||
                                 (mpa.Alias.ToLower() == partNameLower && mpa.Enabled)
                            select mp;

                var part = await query.Include(p => p.Aliases).FirstOrDefaultAsync();

                return part;
            }

            return result;
        }
    }
}
