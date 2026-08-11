using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;

namespace SheetMusic.Api.Sets.Services;

/// <summary>
/// Resolves split PDF parts against the catalog and persists recognized parts on a set.
/// </summary>
public sealed class PdfPartsImportService(SheetMusicContext db, IBlobClient blobClient, SheetMusicAgent agent)
{
    /// <summary>
    /// Imports recognized PDF part groups into <paramref name="set"/>.
    /// </summary>
    public async Task ImportAsync(SheetMusicSet set, PdfPartSplitResult split, CancellationToken cancellationToken)
    {
        var candidates = await db.MusicParts.AsNoTracking().Select(part => part.Name).ToListAsync(cancellationToken);
        var unresolved = new List<string>();

        foreach (var group in split.Groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var detectedName = group.NormalizedPartName;
            if (string.Equals(detectedName, "UNRECOGNIZED", StringComparison.Ordinal))
            {
                unresolved.Add(detectedName);
                continue;
            }

            var part = await FindPartAsync(detectedName, cancellationToken);
            var matchedByAi = false;
            if (part is null)
            {
                var match = await agent.ClassifyPartAsync(detectedName, candidates, cancellationToken);
                part = string.IsNullOrWhiteSpace(match) ? null : await FindPartAsync(match, cancellationToken);
                matchedByAi = part is not null;
            }

            if (part is null)
            {
                unresolved.Add(detectedName);
                continue;
            }

            if (matchedByAi && !await db.MusicPartAliases.AnyAsync(alias => alias.MusicPartId == part.Id && alias.Alias.ToLower() == detectedName.ToLower(), cancellationToken))
            {
                db.MusicPartAliases.Add(new MusicPartAlias { Id = Guid.NewGuid(), Alias = detectedName, Enabled = true, MusicPartId = part.Id });
            }

            if (set.Parts.Any(existing => existing.MusicPartId == part.Id))
                continue;

            await blobClient.AddMusicPartContentAsync(new PartRelatedToSet(set.Id, part.Id), new MemoryStream(group.Content!), cancellationToken);
            var setPart = new SheetMusicPart
            {
                Id = Guid.NewGuid(),
                SetId = set.Id,
                MusicPartId = part.Id,
                Source = matchedByAi ? "Ai" : "Human",
                ModelVersion = matchedByAi ? "gpt-5-mini" : null,
                PromptVersion = matchedByAi ? "part-v1" : null,
                SuggestedAt = matchedByAi ? DateTimeOffset.UtcNow : null,
            };
            db.SheetMusicParts.Add(setPart);
            set.Parts.Add(setPart);
        }

        var existingUnresolved = (set.MissingParts ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var allUnresolved = existingUnresolved.Concat(unresolved).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        set.MissingParts = allUnresolved.Count == 0 ? null : string.Join(", ", allUnresolved);
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<MusicPart?> FindPartAsync(string name, CancellationToken cancellationToken) =>
        db.MusicParts.Include(part => part.Aliases).FirstOrDefaultAsync(part =>
            part.Name.ToLower() == name.ToLower() || part.Aliases.Any(alias => alias.Enabled && alias.Alias.ToLower() == name.ToLower()), cancellationToken);
}