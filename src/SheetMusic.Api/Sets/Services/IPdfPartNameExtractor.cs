namespace SheetMusic.Api.Sets.Services;

/// <summary>
/// Extracts sheet-music part names from OCR-recognized page-header text.
/// </summary>
public interface IPdfPartNameExtractor
{
    /// <summary>
    /// Extracts a part name for every value in <paramref name="headerTexts"/>.
    /// </summary>
    Task<IReadOnlyList<string?>> ExtractPartNamesAsync(IReadOnlyList<string> headerTexts, CancellationToken cancellationToken);
}