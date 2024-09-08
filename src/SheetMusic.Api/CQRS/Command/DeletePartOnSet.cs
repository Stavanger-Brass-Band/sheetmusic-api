using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class DeletePartOnSet(string setIdentifier, string partIdentifier) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public string PartIdentifier { get; } = partIdentifier;

    public class Handler(IBlobClient blobClient, SheetMusicContext db, IMediator mediator) : IRequestHandler<DeletePartOnSet>
    {
        public async Task Handle(DeletePartOnSet request, CancellationToken cancellationToken)
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
