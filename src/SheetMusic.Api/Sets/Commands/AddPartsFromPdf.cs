using MediatR;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Errors;
using SheetMusic.Api.Sets.Services;

namespace SheetMusic.Api.Sets.Commands;

/// <summary>
/// Imports parts from a combined PDF score into an existing set.
/// </summary>
public sealed class AddPartsFromPdf(Guid setId, Stream pdfContent) : IRequest
{
    /// <summary>The target set identifier.</summary>
    public Guid SetId { get; } = setId;
    /// <summary>The uploaded combined score PDF.</summary>
    public Stream PdfContent { get; } = pdfContent;

    /// <summary>Handles splitting and importing the PDF parts.</summary>
    public sealed class Handler(SheetMusicContext db, PdfPartSplitter splitter, PdfPartsImportService importer) : IRequestHandler<AddPartsFromPdf>
    {
        /// <inheritdoc />
        public async Task Handle(AddPartsFromPdf request, CancellationToken cancellationToken)
        {
            var set = await db.SheetMusicSets.Include(item => item.Parts).FirstOrDefaultAsync(item => item.Id == request.SetId, cancellationToken)
                ?? throw new NotFoundError(request.SetId.ToString(), "Set was not found");
            var split = await splitter.SplitAsync(request.PdfContent, cancellationToken);
            await importer.ImportAsync(set, split, cancellationToken);
        }
    }
}