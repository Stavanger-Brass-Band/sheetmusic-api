using MediatR;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Errors;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.CQRS.Query;

public class GetPartsZipAsStream(string setIdentifier) : IRequest<Stream>
{
    public string SetIdentifier { get; } = setIdentifier;

    public class Handler(IBlobClient blobClient, IMediator mediator) : IRequestHandler<GetPartsZipAsStream, Stream>
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
                var entry = zip.CreateEntry($"{partRelation.Part.Name}.pdf");
                using var entryStream = entry.Open();
                var id = new PartRelatedToSet(set.Id, partRelation.MusicPartId);
                var contents = await blobClient.GetMusicPartContentStreamAsync(id);
                contents.CopyTo(entryStream);

                await entryStream.FlushAsync(cancellationToken);
            }

            return memstream;
        }
    }
}
