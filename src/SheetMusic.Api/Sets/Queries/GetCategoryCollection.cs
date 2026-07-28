using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.Extensions;
using SheetMusic.Api.OData.MVC;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Queries;

public class GetCategoryCollection(ODataQueryParams queryParams) : IRequest<List<Category>>
{
    public ODataQueryParams QueryParams { get; } = queryParams;

    public class Handler(SheetMusicContext db) : IRequestHandler<GetCategoryCollection, List<Category>>
    {
        public async Task<List<Category>> Handle(GetCategoryCollection request, CancellationToken cancellationToken)
        {
            var query = db.Categories.AsQueryable();

            if (request.QueryParams != null && request.QueryParams.HasFilter)
            {
                query = query.ApplyODataFilters(request.QueryParams, m =>
                {
                    m.MapField("name", c => c.Name);
                    m.MapField("inactive", c => c.Inactive);
                });
            }

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }
    }
}
