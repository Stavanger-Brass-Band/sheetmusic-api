using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class DeletePartOnSet : IRequest
{
    public DeletePartOnSet(string setIdentifier, string partIdentifier)
    {
        SetIdentifier = setIdentifier;
        PartIdentifier = partIdentifier;
    }

    public string SetIdentifier { get; }
    public string PartIdentifier { get; }

    public class Handler : AsyncRequestHandler<DeletePartOnSet>
    {
        private readonly IBlobClient blobClient;
        private readonly SheetMusicContext db;
        private readonly IMediator mediator;

        public Handler(IBlobClient blobClient, SheetMusicContext db, IMediator mediator)
        {
            this.blobClient = blobClient;
            this.db = db;
            this.mediator = mediator;
        }

        protected override async Task Handle(DeletePartOnSet request, CancellationToken cancellationToken)
        {
            var partOnSet = await mediator.Send(new GetPartOnSet(request.SetIdentifier, request.PartIdentifier), cancellationToken);
            if (partOnSet is null) throw new NotFoundError($"{request.SetIdentifier}/{request.PartIdentifier}");

            var blobIdentifer = new PartRelatedToSet(partOnSet.SetId, partOnSet.MusicPartId);
            await blobClient.DeletePartContentAsync(blobIdentifer);

            if (partOnSet is not null)
            {
                db.Remove(partOnSet);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
