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

namespace SheetMusic.Api.Projects.Queries;

public class GetProjectCollection(ODataQueryParams? queryParams) : IRequest<List<Project>>
{
    public ODataQueryParams? QueryParams { get; } = queryParams;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetProjectCollection, List<Project>>
    {
        private static readonly Action<ODataFieldMapping<Project>> FieldMapping = m =>
        {
            m.MapField("startDate", p => p.StartDate);
            m.MapField("endDate", p => p.EndDate);
            m.MapField("name", p => p.Name);
        };

        public async Task<List<Project>> Handle(GetProjectCollection request, CancellationToken cancellationToken)
        {
            var query = db.Projects.AsQueryable();

            if (request.QueryParams != null && request.QueryParams.HasFilter)
                query = query.ApplyODataFilters(request.QueryParams, FieldMapping);

            if (request.QueryParams != null && request.QueryParams.OrderBy.Any())
                query = query.ApplyODataOrderBy(request.QueryParams, FieldMapping);

            if (request.QueryParams?.Skip is not null)
                query = query.Skip(request.QueryParams.Skip.Value);

            if (request.QueryParams?.Top is not null)
                query = query.Take(request.QueryParams.Top.Value);

            return await query.ToListAsync(cancellationToken);
        }
    }
}
