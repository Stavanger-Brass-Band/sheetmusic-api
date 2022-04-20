using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class UpdatePart : IRequest
{
    public UpdatePart(Guid partId, string name, int sortOrder, bool indexable)
    {
        PartId = partId;
        Name = name;
        SortOrder = sortOrder;
        Indexable = indexable;
    }

    public Guid PartId { get; }

    public string Name { get; }
    public int SortOrder { get; }

    public bool Indexable { get; }

    public class Handler : AsyncRequestHandler<UpdatePart>
    {
        private readonly SheetMusicContext db;
        private readonly IMediator mediator;

        public Handler(SheetMusicContext db, IMediator mediator)
        {
            this.db = db;
            this.mediator = mediator;
        }

        protected override async Task Handle(UpdatePart request, CancellationToken cancellationToken)
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
