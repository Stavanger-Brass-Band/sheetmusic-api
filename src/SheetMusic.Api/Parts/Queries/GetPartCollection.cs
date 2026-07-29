using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.Expression;
using SheetMusic.Api.OData.Extensions;
using SheetMusic.Api.OData.MVC;
using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Parts.Queries;

public class GetPartCollection(ODataQueryParams queryParams) : IRequest<List<MusicPart>>
{
    public ODataQueryParams QueryParams { get; } = queryParams;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetPartCollection, List<MusicPart>>
    {
        private static readonly Action<ODataFieldMapping<MusicPart>> FieldMapping = m =>
        {
            m.MapField("name", p => p.Name);
            m.MapField("indexable", p => p.Indexable);
            m.MapField("sortOrder", p => p.SortOrder);
        };

        public async Task<List<MusicPart>> Handle(GetPartCollection request, CancellationToken cancellationToken)
        {
            var query = db.MusicParts.AsQueryable();

            if (request.QueryParams != null &&
                request.QueryParams.Expand.Any(e => string.Equals(e, "aliases", StringComparison.OrdinalIgnoreCase)))
            {
                query = query.Include(p => p.Aliases);
            }

            if (request.QueryParams != null && request.QueryParams.HasFilter)
                query = query.ApplyODataFilters(request.QueryParams, FieldMapping);

            if (request.QueryParams != null && request.QueryParams.OrderBy.Any())
                query = query.ApplyODataOrderBy(request.QueryParams, FieldMapping);

            if (request.QueryParams?.Skip is not null)
                query = query.Skip(request.QueryParams.Skip.Value);

            if (request.QueryParams?.Top is not null)
                query = query.Take(request.QueryParams.Top.Value);

            var results = await query.ToListAsync(cancellationToken);

            return results;
        }
    }
}
