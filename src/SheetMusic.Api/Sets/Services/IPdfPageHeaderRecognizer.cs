namespace SheetMusic.Api.Sets.Services;

/// <summary>
/// Recognizes the part header on every page of a PDF score.
/// </summary>
public interface IPdfPageHeaderRecognizer
{
    /// <summary>
    /// Recognizes headers from <paramref name="pdfContent"/>.
    /// </summary>
    Task<IReadOnlyList<PdfPageHeader>> RecognizeAsync(ReadOnlyMemory<byte> pdfContent, CancellationToken cancellationToken);
}