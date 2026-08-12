namespace SheetMusic.Api.Sets.Services;

/// <summary>
/// Extracts set metadata from OCR-recognized score headers.
/// </summary>
public interface IPdfSetMetadataExtractor
{
    /// <summary>
    /// Extracts the title, composer, and arranger from the supplied headers.
    /// </summary>
    Task<PdfSetMetadata?> ExtractAsync(IReadOnlyList<string> headers, CancellationToken cancellationToken);
}

/// <summary>
/// Metadata inferred from a combined score PDF.
/// </summary>
public sealed record PdfSetMetadata(string Title, string? Composer, string? Arranger);