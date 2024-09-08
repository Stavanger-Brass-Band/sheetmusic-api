using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetPartOnSet(string setIdentifier, string partIdentifier) : IRequest<SheetMusicPart?>
{
    public string SetIdentifier { get; } = setIdentifier;
    public string PartIdentifier { get; } = partIdentifier;

    public class Handler(IMediator mediator, SheetMusicContext db) : IRequestHandler<GetPartOnSet, SheetMusicPart?>
    {
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
