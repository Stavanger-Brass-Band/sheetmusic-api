using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.MVC;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Projects;

public class GetProjectCollection(ODataQueryParams? queryParams) : IRequest<List<Project>>
{
    public ODataQueryParams? QueryParams { get; } = queryParams;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetProjectCollection, List<Project>>
    {
        public async Task<List<Project>> Handle(GetProjectCollection request, CancellationToken cancellationToken)
        {
            var query = db.Projects.AsQueryable();

            if (request.QueryParams != null && request.QueryParams.HasFilter)
            {
                query = query.ApplyODataFilters(request.QueryParams, m =>
                {
                    m.MapField("startDate", p => p.StartDate);
                    m.MapField("endDate", p => p.EndDate);
                    m.MapField("name", p => p.Name);
                });
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
