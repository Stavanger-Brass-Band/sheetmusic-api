using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetPartsForSet(Guid setId) : IRequest<List<SheetMusicPart>>
{
    public Guid SetId { get; } = setId;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetPartsForSet, List<SheetMusicPart>>
    {
        public async Task<List<SheetMusicPart>> Handle(GetPartsForSet request, CancellationToken cancellationToken)
        {
            var query = from set in db.SheetMusicSets
                        from setPart in set.Parts
                        where set.Id == request.SetId
                        select setPart;

            var items = await query
                .Include(sp => sp.Set)
                .Include(sp => sp.Part)
                .OrderBy(sp => sp.Part.SortOrder)
                .ThenBy(sp => sp.Part.Name)
                .ToListAsync(cancellationToken: cancellationToken);

            return items.ToList();
        }
    }
}
