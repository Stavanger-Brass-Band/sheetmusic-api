using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query
{
    public class GetPartOnSet : IRequest<SheetMusicPart?>
    {
        public GetPartOnSet(string setIdentifier, string partIdentifier)
        {
            SetIdentifier = setIdentifier;
            PartIdentifier = partIdentifier;
        }

        public string SetIdentifier { get; }
        public string PartIdentifier { get; }

        public class Handler : IRequestHandler<GetPartOnSet, SheetMusicPart?>
        {
            private readonly IMediator mediator;
            private readonly SheetMusicContext db;

            public Handler(IMediator mediator, SheetMusicContext db)
            {
                this.mediator = mediator;
                this.db = db;
            }

            public async Task<SheetMusicPart?> Handle(GetPartOnSet request, CancellationToken cancellationToken)
            {
                var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);
                if (set is null) throw new NotFoundError(request.SetIdentifier);

                var part = await mediator.Send(new GetMusicPart(request.PartIdentifier), cancellationToken);
                if (part is null) throw new NotFoundError(request.PartIdentifier);

                return await db.SheetMusicParts
                    .Include(smp => smp.Part)
                    .FirstOrDefaultAsync(smp => smp.SetId == set.Id && smp.MusicPartId == part.Id, cancellationToken: cancellationToken);
            }
        }
    }
}
