using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Sets;
using SheetMusic.Api.Users.Authorization;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Queries;

public class GetPartsZipAsStream(string setIdentifier, Guid userId) : IRequest<Stream>
{
    public string SetIdentifier { get; } = setIdentifier;
    public Guid UserId { get; } = userId;

    public class Handler(IBlobClient blobClient, IMediator mediator, CatalogAccessService catalogAccess) : IRequestHandler<GetPartsZipAsStream, Stream>
    {
        public async Task<Stream> Handle(GetPartsZipAsStream request, CancellationToken cancellationToken)
        {
            var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);

            if (set is null)
                throw new NotFoundError(request.SetIdentifier, "Set was not found");

            var memstream = new MemoryStream();

            using var zip = new ZipArchive(memstream, ZipArchiveMode.Create, true);
            foreach (var partRelation in set.Parts)
            {
                if (!await catalogAccess.CanUserAccessPartAsync(request.UserId, set.Id, partRelation.MusicPartId, cancellationToken))
                    continue;

                var entry = zip.CreateEntry($"{partRelation.Part.Name}.pdf");
                using var entryStream = entry.Open();
                var id = new PartRelatedToSet(set.Id, partRelation.MusicPartId);
                await using var contents = await blobClient.GetMusicPartContentStreamAsync(id, cancellationToken);
                await contents.CopyToAsync(entryStream, cancellationToken);

                await entryStream.FlushAsync(cancellationToken);
            }

            return memstream;
        }
    }
}
