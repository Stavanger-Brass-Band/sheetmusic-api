using MediatR;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.CQRS.Query;
using SheetMusic.Api.Errors;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Command;

public class AddPartsContentForSet(string setIdentifier, Stream zipFileStream) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public Stream ZipFileStream { get; } = zipFileStream;

    public class Handler(ILogger<AddPartsContentForSet.Handler> logger, IMediator mediator) : IRequestHandler<AddPartsContentForSet>
    {
        public async Task Handle(AddPartsContentForSet request, CancellationToken cancellationToken)
        {
            var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);

            if (set is null)
                throw new NotFoundError(request.SetIdentifier, "Set was not found");

            logger.LogInformation($"Resolver identifier '{request.SetIdentifier}' as set '{set.Id}'");

            using var zipArchive = new ZipArchive(request.ZipFileStream);
            foreach (var entry in zipArchive.Entries)
            {
                logger.LogInformation($"Processing entry {entry.Name}");

                var partName = Path.GetFileNameWithoutExtension(entry.Name);
                var part = await mediator.Send(new GetMusicPart(partName), cancellationToken) ?? await mediator.Send(new AddPart(partName, 99, true), cancellationToken);

                if (set.Parts.Any(sp => sp.MusicPartId == part.Id))
                    throw new MusicSetPartAlreadyAddedError(set.Title, part.Name);

                logger.LogInformation($"Part identified as {part.Name}. Uploading.");

                using var entryStream = entry.Open();
                await mediator.Send(new AddPartOnSet(set.Id.ToString(), part.Id.ToString(), entryStream), cancellationToken);

                logger.LogInformation($"Part '{part.Name}' successfully added to set '{set.Title}'");
            }
        }
    }
}

