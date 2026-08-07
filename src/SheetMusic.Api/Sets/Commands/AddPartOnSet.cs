using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Parts.Queries;
using SheetMusic.Api.Sets;
using SheetMusic.Api.Sets.Errors;
using SheetMusic.Api.Sets.Queries;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Commands;

public class AddPartOnSet(
    string setIdentifier,
    string partIdentifier,
    Stream content,
    string source = "Human",
    string? modelVersion = null,
    string? promptVersion = null) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public string PartIdentifier { get; } = partIdentifier;
    public Stream Content { get; } = content;
    public string Source { get; } = source;
    public string? ModelVersion { get; } = modelVersion;
    public string? PromptVersion { get; } = promptVersion;

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

            await blobClient.AddMusicPartContentAsync(new PartRelatedToSet(set.Id, part.Id), request.Content, cancellationToken);

            db.SheetMusicParts.Add(new SheetMusicPart
            {
                Id = Guid.NewGuid(),
                MusicPartId = part.Id,
                SetId = set.Id,
                Source = request.Source,
                ModelVersion = request.ModelVersion,
                PromptVersion = request.PromptVersion,
                SuggestedAt = request.Source == "Ai" ? DateTimeOffset.UtcNow : null,
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
