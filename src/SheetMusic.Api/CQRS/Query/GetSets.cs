using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.OData;
using SheetMusic.Api.OData.MVC;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query
{
    public class GetSets : IRequest<List<SheetMusicSet>>
    {
        public GetSets(ODataQueryParams queryParams)
        {
            QueryParams = queryParams;
        }

        public ODataQueryParams QueryParams { get; }

        public class Handler : IRequestHandler<GetSets, List<SheetMusicSet>>
        {
            private readonly SheetMusicContext db;

            public Handler(SheetMusicContext db)
            {
                this.db = db;
            }

            public async Task<List<SheetMusicSet>> Handle(GetSets request, CancellationToken cancellationToken)
            {
                var baseQuery = db.SheetMusicSets.AsQueryable();

                if (request.QueryParams.HasFilter)
                {
                    baseQuery = baseQuery.ApplyODataFilters(request.QueryParams, m =>
                    {
                        m.MapField("archiveNumber", s => s.ArchiveNumber);
                        m.MapField("title", s => s.Title);
                        m.MapField("composer", s => s.Composer);
                        m.MapField("arranger", s => s.Arranger);
                        m.MapField("soleSellingAgent", s => s.SoleSellingAgent);
                    });
                }

                if (request.QueryParams.Expand.Contains("parts"))
                {
                    baseQuery = baseQuery.Include(s => s.Parts);
                }

                return await baseQuery.ToListAsync(cancellationToken);
            }
        }
    }
}
