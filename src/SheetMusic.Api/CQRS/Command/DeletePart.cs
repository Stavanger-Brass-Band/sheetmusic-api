using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command
{
    public class DeletePart : IRequest
    {
        public DeletePart(Guid partId)
        {
            PartId = partId;
        }

        public Guid PartId { get; }

        public class Handler : AsyncRequestHandler<DeletePart>
        {
            private readonly SheetMusicContext db;

            public Handler(SheetMusicContext db)
            {
                this.db = db;
            }

            protected override async Task Handle(DeletePart request, CancellationToken cancellationToken)
            {
                var part = await db.MusicParts.FirstOrDefaultAsync(p => p.Id == request.PartId, cancellationToken: cancellationToken);

                if (part == null) throw new NotFoundError(request.PartId.ToString(), "Part not found");

                if (part.MusicianMusicParts.Any() || part.Parts.Any())
                    throw new InvalidOperationException("Part is linked to musicians or sets and cannot be deleted");

                db.MusicParts.Remove(part);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
