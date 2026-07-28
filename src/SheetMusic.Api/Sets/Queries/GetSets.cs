using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.Expression;
using SheetMusic.Api.OData.Extensions;
using SheetMusic.Api.OData.MVC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Queries;

public class GetSets(ODataQueryParams queryParams, Guid? categoryId = null) : IRequest<List<SheetMusicSet>>
{
    public ODataQueryParams QueryParams { get; } = queryParams;
    public Guid? CategoryId { get; } = categoryId;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetSets, List<SheetMusicSet>>
    {
        private static readonly Action<ODataFieldMapping<SheetMusicSet>> FieldMapping = m =>
        {
            m.MapField("archiveNumber", s => s.ArchiveNumber);
            m.MapField("title", s => s.Title);
            m.MapField("composer", s => s.Composer);
            m.MapField("arranger", s => s.Arranger);
            m.MapField("soleSellingAgent", s => s.SoleSellingAgent);
        };

        public async Task<List<SheetMusicSet>> Handle(GetSets request, CancellationToken cancellationToken)
        {
            var baseQuery = db.SheetMusicSets.AsQueryable();

            if (request.QueryParams.HasFilter)
                baseQuery = baseQuery.ApplyODataFilters(request.QueryParams, FieldMapping);

            if (request.QueryParams.HasSearch)
            {
                var term = request.QueryParams.Search ?? "";

                baseQuery = baseQuery.Where(set =>
                    set.ArchiveNumber.ToString().Contains(term) ||
                    set.Title.Contains(term) ||
                    (set.Arranger != null && set.Arranger.Contains(term)) ||
                    (set.Composer != null && set.Composer.Contains(term)));
            }

            if (request.CategoryId.HasValue)
                baseQuery = baseQuery.Where(set => set.Categories.Any(c => c.CategoryId == request.CategoryId.Value));

            baseQuery = request.QueryParams.OrderBy.Any()
                ? baseQuery.ApplyODataOrderBy(request.QueryParams, FieldMapping)
                : baseQuery.OrderBy(s => s.ArchiveNumber);

            if (request.QueryParams.Skip.HasValue)
                baseQuery = baseQuery.Skip(request.QueryParams.Skip.Value);

            if (request.QueryParams.Top.HasValue)
                baseQuery = baseQuery.Take(request.QueryParams.Top.Value);

            return await baseQuery
                .Include(s => s.Parts)
                    .ThenInclude(p => p.Part)
                .Include(s => s.Categories)
                    .ThenInclude(c => c.Category)
                .ToListAsync(cancellationToken);
        }
    }
}
