using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Search;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command
{
    public class BuildPartIndex : IRequest
    {
        public class Handler : AsyncRequestHandler<BuildPartIndex>
        {
            private readonly IIndexAdminService indexAdminService;
            private readonly SheetMusicContext db;

            public Handler(IIndexAdminService indexAdminService, SheetMusicContext db)
            {
                this.indexAdminService = indexAdminService;
                this.db = db;
            }

            protected override async Task Handle(BuildPartIndex request, CancellationToken cancellationToken)
            {
                await indexAdminService.ClearIndexAsync<PartIndex>();
                await indexAdminService.EnsureIndexAsync<PartIndex>();

                var dbParts = await db.MusicParts
                    .Include(mp => mp.Aliases)
                    .ToListAsync(cancellationToken: cancellationToken);

                var indexParts = dbParts
                    .Where(p => p.Indexable)
                    .Select(p => new PartIndex
                    {
                        Id = p.Id.ToString(),
                        PartName = p.Name,
                        Aliases = p.Aliases
                        .Where(a => a.Enabled)
                        .Select(pa => pa.Alias)
                        .ToArray()
                    }).ToList();

                await indexAdminService.FillIndexAsync(indexParts);
            }
        }
    }
}
