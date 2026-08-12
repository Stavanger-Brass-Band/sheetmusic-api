using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

/// <summary>
/// Thrown when Document Intelligence cannot process a PDF.
/// </summary>
public sealed class OcrUnavailableError(Exception innerException) : ExceptionBase("Document Intelligence OCR is currently unavailable.", innerException)
{
    /// <inheritdoc />
    public override HttpStatusCode StatusCode => HttpStatusCode.ServiceUnavailable;
}