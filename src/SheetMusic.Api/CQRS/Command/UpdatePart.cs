using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class UpdatePart(Guid partId, string name, int sortOrder, bool indexable) : IRequest
{
    public Guid PartId { get; } = partId;

    public string Name { get; } = name;
    public int SortOrder { get; } = sortOrder;

    public bool Indexable { get; } = indexable;

    public class Handler(SheetMusicContext db, IMediator mediator) : IRequestHandler<UpdatePart>
    {
        public async Task Handle(UpdatePart request, CancellationToken cancellationToken)
        {
            var existingPart = await db.MusicParts.FirstOrDefaultAsync(p => p.Id == request.PartId);

            if (existingPart == null)
                throw new NotFoundError(request.PartId.ToString(), "Part not found");


            existingPart.Name = request.Name;
            existingPart.SortOrder = request.SortOrder;
            existingPart.Indexable = request.Indexable;

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
                await mediator.Send(new BuildPartIndex());
            }
        }
    }
}
