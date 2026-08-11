using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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
        var parts = await db.MusicParts.Include(part => part.Aliases).ToListAsync(cancellationToken);
        var candidates = parts.Select(part => part.Name).ToList();
        var unresolved = new List<string>();
        var pendingParts = new Dictionary<Guid, PendingPart>();

        foreach (var group in split.Groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var detectedName = group.NormalizedPartName;
            if (string.Equals(detectedName, "UNRECOGNIZED", StringComparison.Ordinal))
            {
                unresolved.Add(detectedName);
                continue;
            }

            var part = FindPart(parts, detectedName);
            var matchedByAi = false;
            if (part is null)
            {
                var match = await agent.ClassifyPartAsync(detectedName, candidates, cancellationToken);
                part = string.IsNullOrWhiteSpace(match) ? null : FindPart(parts, match);
                matchedByAi = part is not null;
            }

            if (part is null)
            {
                unresolved.Add(detectedName);
                continue;
            }

            if (matchedByAi && !part.Aliases.Any(alias => string.Equals(alias.Alias, detectedName, StringComparison.OrdinalIgnoreCase)))
            {
                var alias = new MusicPartAlias { Id = Guid.NewGuid(), Alias = detectedName, Enabled = true, MusicPartId = part.Id };
                db.MusicPartAliases.Add(alias);
                part.Aliases.Add(alias);
            }

            if (set.Parts.Any(existing => existing.MusicPartId == part.Id))
                continue;

            if (pendingParts.TryGetValue(part.Id, out var pendingPart))
            {
                pendingPart.Content.Add(group.Content!);
                pendingPart.MatchedByAi |= matchedByAi;
                continue;
            }

            pendingParts.Add(part.Id, new PendingPart(part, matchedByAi, [group.Content!]));
        }

        foreach (var pendingPart in pendingParts.Values)
        {
            await blobClient.AddMusicPartContentAsync(new PartRelatedToSet(set.Id, pendingPart.Part.Id), new MemoryStream(CombinePdfContent(pendingPart.Content)), cancellationToken);
            var setPart = new SheetMusicPart
            {
                Id = Guid.NewGuid(),
                SetId = set.Id,
                MusicPartId = pendingPart.Part.Id,
                Source = pendingPart.MatchedByAi ? "Ai" : "Human",
                ModelVersion = pendingPart.MatchedByAi ? "gpt-5-mini" : null,
                PromptVersion = pendingPart.MatchedByAi ? "part-v1" : null,
                SuggestedAt = pendingPart.MatchedByAi ? DateTimeOffset.UtcNow : null,
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

    private static MusicPart? FindPart(IEnumerable<MusicPart> parts, string name) =>
        parts.FirstOrDefault(part =>
            string.Equals(part.Name, name, StringComparison.OrdinalIgnoreCase) ||
            part.Aliases.Any(alias => alias.Enabled && string.Equals(alias.Alias, name, StringComparison.OrdinalIgnoreCase)));

    private static byte[] CombinePdfContent(IReadOnlyList<byte[]> content)
    {
        if (content.Count == 1)
            return content[0];

        using var combined = new PdfDocument();
        foreach (var partContent in content)
        {
            using var source = PdfReader.Open(new MemoryStream(partContent), PdfDocumentOpenMode.Import);
            foreach (var page in source.Pages)
                combined.AddPage(page);
        }

        using var output = new MemoryStream();
        combined.Save(output, false);
        return output.ToArray();
    }

    private sealed class PendingPart(MusicPart part, bool matchedByAi, List<byte[]> content)
    {
        public MusicPart Part { get; } = part;
        public bool MatchedByAi { get; set; } = matchedByAi;
        public List<byte[]> Content { get; } = content;
    }
}