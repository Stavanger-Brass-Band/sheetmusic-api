using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command
{
    public class DeleteSet : IRequest
    {
        public DeleteSet(string setIdentifier)
        {
            SetIdentifier = setIdentifier;
        }

        public string SetIdentifier { get; }

        public class Handler : AsyncRequestHandler<DeleteSet>
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

            protected override async Task Handle(DeleteSet request, CancellationToken cancellationToken)
            {
                var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);

                if (set is null) throw new NotFoundError(request.SetIdentifier, "Set was not found");

                await blobClient.DeleteSetContentAsync(set.Id);
                db.SheetMusicSets.Remove(set);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
