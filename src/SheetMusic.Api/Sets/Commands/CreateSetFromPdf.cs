using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Sets.Errors;
using SheetMusic.Api.Sets.Services;

namespace SheetMusic.Api.Sets.Commands;

/// <summary>
/// Creates a set and imports its parts from a combined PDF score.
/// </summary>
public sealed class CreateSetFromPdf(Stream pdfContent) : IRequest<SheetMusicSet>
{
    /// <summary>The uploaded combined score PDF.</summary>
    public Stream PdfContent { get; } = pdfContent;

    /// <summary>Handles metadata extraction, archive allocation, and part import.</summary>
    public sealed class Handler(SheetMusicContext db, IBlobClient blobClient, PdfPartSplitter splitter, IPdfSetMetadataExtractor metadataExtractor, PdfPartsImportService importer) : IRequestHandler<CreateSetFromPdf, SheetMusicSet>
    {
        /// <inheritdoc />
        public async Task<SheetMusicSet> Handle(CreateSetFromPdf request, CancellationToken cancellationToken)
        {
            var split = await splitter.SplitAsync(request.PdfContent, cancellationToken);
            var metadata = await metadataExtractor.ExtractAsync(split.SourceHeaders
                .Select(header => header.NormalizedPartName)
                .Where(header => !string.IsNullOrWhiteSpace(header))
                .Cast<string>()
                .ToList(), cancellationToken);
            if (metadata is null || string.IsNullOrWhiteSpace(metadata.Title))
                throw new PdfSetMetadataError();

            var archiveNumber = await db.SheetMusicSets.AnyAsync(cancellationToken)
                ? await db.SheetMusicSets.MaxAsync(set => set.ArchiveNumber, cancellationToken) + 1
                : 1;
            var set = new SheetMusicSet(archiveNumber, metadata.Title) { Composer = metadata.Composer, Arranger = metadata.Arranger };
            db.SheetMusicSets.Add(set);
            await db.SaveChangesAsync(cancellationToken);
            try
            {
                await importer.ImportAsync(set, split, cancellationToken);
            }
            catch
            {
                await blobClient.DeleteSetContentAsync(set.Id);
                foreach (var alias in db.ChangeTracker.Entries<MusicPartAlias>().Where(entry => entry.State == EntityState.Added))
                    alias.State = EntityState.Detached;
                db.SheetMusicSets.Remove(set);
                await db.SaveChangesAsync(CancellationToken.None);
                throw;
            }
            return set;
        }
    }
}