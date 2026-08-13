using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Parts.Queries;
using SheetMusic.Api.Sets.Errors;
using SheetMusic.Api.Sets.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Commands;

public class ChangePartOnSet(string setIdentifier, string currentPartIdentifier, string replacementPartIdentifier) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public string CurrentPartIdentifier { get; } = currentPartIdentifier;
    public string ReplacementPartIdentifier { get; } = replacementPartIdentifier;

    public class Handler(SheetMusicContext db, IMediator mediator, IBlobClient blobClient) : IRequestHandler<ChangePartOnSet>
    {
        public async Task Handle(ChangePartOnSet request, CancellationToken cancellationToken)
        {
            var partOnSet = await mediator.Send(new GetPartOnSet(request.SetIdentifier, request.CurrentPartIdentifier), cancellationToken)
                ?? throw new NotFoundError($"{request.SetIdentifier}/{request.CurrentPartIdentifier}", "Part is not assigned to the set");

            var replacementPart = await mediator.Send(new GetMusicPart(request.ReplacementPartIdentifier), cancellationToken)
                ?? throw new NotFoundError(request.ReplacementPartIdentifier, "Replacement part was not found");

            if (await db.SheetMusicParts.AnyAsync(part => part.SetId == partOnSet.SetId && part.MusicPartId == replacementPart.Id, cancellationToken))
            {
                var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken)
                    ?? throw new NotFoundError(request.SetIdentifier, "Set was not found");
                throw new MusicSetPartAlreadyAddedError(set.Title, replacementPart.Name);
            }

            var currentBlob = new PartRelatedToSet(partOnSet.SetId, partOnSet.MusicPartId);
            var replacementBlob = new PartRelatedToSet(partOnSet.SetId, replacementPart.Id);

            await using var currentContent = await blobClient.GetMusicPartContentStreamAsync(currentBlob);
            await blobClient.AddMusicPartContentAsync(replacementBlob, currentContent, cancellationToken);

            try
            {
                partOnSet.MusicPartId = replacementPart.Id;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await blobClient.DeletePartContentAsync(replacementBlob);
                throw;
            }

            await blobClient.DeletePartContentAsync(currentBlob);
        }
    }
}