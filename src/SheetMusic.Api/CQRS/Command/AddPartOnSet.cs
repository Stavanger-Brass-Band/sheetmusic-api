using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class AddPartOnSet(string setIdentifier, string partIdentifier, Stream content) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public string PartIdentifier { get; } = partIdentifier;
    public Stream Content { get; } = content;

    public class Handler(SheetMusicContext db, IMediator mediator, IBlobClient blobClient) : IRequestHandler<AddPartOnSet>
    {
        public async Task Handle(AddPartOnSet request, CancellationToken cancellationToken)
        {
            var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);
            if (set is null) throw new NotFoundError(request.SetIdentifier, "Set was not found");

            var part = await mediator.Send(new GetMusicPart(request.PartIdentifier), cancellationToken);
            if (part is null) throw new NotFoundError(request.PartIdentifier, "Part was not found");

            var partOnSet = await mediator.Send(new GetPartOnSet(request.SetIdentifier, request.PartIdentifier), cancellationToken);

            if (partOnSet is not null)
                throw new MusicSetPartAlreadyAddedError(set.Title, part.Name);

            await blobClient.AddMusicPartContentAsync(new PartRelatedToSet(set.Id, part.Id), request.Content);

            db.SheetMusicParts.Add(new SheetMusicPart
            {
                Id = Guid.NewGuid(),
                MusicPartId = part.Id,
                SetId = set.Id
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
