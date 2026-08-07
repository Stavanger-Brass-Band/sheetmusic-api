using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Parts.Commands;

public class UpdatePart(Guid partId, string name, int sortOrder, bool indexable, InstrumentGroup? instrumentGroup) : IRequest
{
    public Guid PartId { get; } = partId;

    public string Name { get; } = name;
    public int SortOrder { get; } = sortOrder;

    public bool Indexable { get; } = indexable;
    public InstrumentGroup? InstrumentGroup { get; } = instrumentGroup;

    public class Handler(SheetMusicContext db, IMediator mediator) : IRequestHandler<UpdatePart>
    {
        public async Task Handle(UpdatePart request, CancellationToken cancellationToken)
        {
            var existingPart = await db.MusicParts.FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken);

            if (existingPart == null)
                throw new NotFoundError(request.PartId.ToString(), "Part not found");


            existingPart.Name = request.Name;
            existingPart.SortOrder = request.SortOrder;
            existingPart.Indexable = request.Indexable;
            existingPart.InstrumentGroup = request.InstrumentGroup;

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync(cancellationToken);
                await mediator.Send(new BuildPartIndex(), cancellationToken);
            }
        }
    }
}
