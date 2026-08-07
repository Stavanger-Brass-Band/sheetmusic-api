using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Parts.Queries;
using SheetMusic.Api.Sets.Errors;
using SheetMusic.Api.Sets.Queries;
using SheetMusic.Api.Sets.Services;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Sets.Commands;

public class AddPartsContentForSet(string setIdentifier, Stream zipFileStream) : IRequest
{
    public string SetIdentifier { get; } = setIdentifier;
    public Stream ZipFileStream { get; } = zipFileStream;

    public class Handler(
        ILogger<AddPartsContentForSet.Handler> logger,
        IMediator mediator,
        SheetMusicContext db,
        SheetMusicAgent agent) : IRequestHandler<AddPartsContentForSet>
    {
        public async Task Handle(AddPartsContentForSet request, CancellationToken cancellationToken)
        {
            var set = await mediator.Send(new GetSet(request.SetIdentifier), cancellationToken);

            if (set is null)
                throw new NotFoundError(request.SetIdentifier, "Set was not found");

            logger.LogInformation($"Resolver identifier '{request.SetIdentifier}' as set '{set.Id}'");

            var unresolvedParts = (set.MissingParts ?? string.Empty)
                .Split(',')
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .ToList();

            var unresolvedPartsChanged = false;
            var candidateNames = await db.MusicParts
                .AsNoTracking()
                .Select(part => part.Name)
                .ToListAsync(cancellationToken);

            using var zipArchive = new ZipArchive(request.ZipFileStream);
            foreach (var entry in zipArchive.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    logger.LogInformation("Skipping zip directory entry {EntryFullName}", entry.FullName);
                    continue;
                }

                logger.LogInformation($"Processing entry {entry.Name}");

                var partName = Path.GetFileNameWithoutExtension(entry.Name);
                var part = await mediator.Send(new GetMusicPart(partName), cancellationToken);
                var matchedByAi = false;

                if (part is null)
                {
                    var modelMatch = await agent.ClassifyPartAsync(partName, candidateNames, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(modelMatch))
                    {
                        part = await mediator.Send(new GetMusicPart(modelMatch), cancellationToken);
                        if (part is not null)
                        {
                            matchedByAi = true;
                            var aliasExists = await db.MusicPartAliases.AnyAsync(alias =>
                                alias.MusicPartId == part.Id && alias.Alias.ToLower() == partName.ToLower(), cancellationToken);
                            if (!aliasExists)
                            {
                                db.MusicPartAliases.Add(new MusicPartAlias
                                {
                                    Id = Guid.NewGuid(),
                                    Alias = partName,
                                    Enabled = true,
                                    MusicPartId = part.Id,
                                });
                                await db.SaveChangesAsync(cancellationToken);
                            }
                        }
                    }

                    if (part is null)
                    {
                        if (!unresolvedParts.Any(p => string.Equals(p, partName, System.StringComparison.OrdinalIgnoreCase)))
                        {
                            unresolvedParts.Add(partName);
                            unresolvedPartsChanged = true;
                        }

                        logger.LogWarning("Could not match zip entry {EntryName} to a known part. Marking as unresolved.", entry.Name);
                        continue;
                    }
                }

                if (set.Parts.Any(sp => sp.MusicPartId == part.Id))
                    throw new MusicSetPartAlreadyAddedError(set.Title, part.Name);

                logger.LogInformation($"Part identified as {part.Name}. Uploading.");

                using var entryStream = entry.Open();
                await mediator.Send(new AddPartOnSet(
                    set.Id.ToString(),
                    part.Id.ToString(),
                    entryStream,
                    matchedByAi ? "Ai" : "Human",
                    matchedByAi ? "gpt-5-mini" : null,
                    matchedByAi ? "part-v1" : null), cancellationToken);

                if (unresolvedParts.RemoveAll(part => string.Equals(part, partName, StringComparison.OrdinalIgnoreCase)) > 0)
                    unresolvedPartsChanged = true;

                logger.LogInformation($"Part '{part.Name}' successfully added to set '{set.Title}'");
            }

            if (unresolvedPartsChanged)
            {
                set.MissingParts = string.Join(", ", unresolvedParts);
                await db.SaveChangesAsync(cancellationToken);
            }
        }
    }
}

