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

public class GetPartsForSet : IRequest<List<SheetMusicPart>>
{
    public GetPartsForSet(Guid setId)
    {
        SetId = setId;
    }

    public Guid SetId { get; }

    public class Handler : IRequestHandler<GetPartsForSet, List<SheetMusicPart>>
    {
        private readonly SheetMusicContext db;

        public Handler(SheetMusicContext db)
        {
            this.db = db;
        }

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
