using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.MVC;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetPartCollection : IRequest<List<MusicPart>>
{
    public GetPartCollection(ODataQueryParams queryParams)
    {
        QueryParams = queryParams;
    }

    public ODataQueryParams QueryParams { get; }

    public class Handler : IRequestHandler<GetPartCollection, List<MusicPart>>
    {
        private readonly SheetMusicContext db;

        public Handler(SheetMusicContext db)
        {
            this.db = db;
        }
        public async Task<List<MusicPart>> Handle(GetPartCollection request, CancellationToken cancellationToken)
        {
            var query = db.MusicParts.AsQueryable();

            if (request.QueryParams != null && request.QueryParams.HasFilter)
            {
                query = query.ApplyODataFilters(request.QueryParams, m =>
                {
                    m.MapField("name", p => p.Name);
                    m.MapField("indexable", p => p.Indexable);
                    m.MapField("sortOrder", p => p.SortOrder);
                });
            }

            var results = await query.ToListAsync();

            return results;
        }
    }
}
