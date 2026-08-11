using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

/// <summary>
/// Thrown when the Document Intelligence endpoint is unavailable to the PDF splitter.
/// </summary>
public sealed class OcrConfigurationError() : ExceptionBase("Document Intelligence OCR is not configured.")
{
    /// <inheritdoc />
    public override HttpStatusCode StatusCode => HttpStatusCode.ServiceUnavailable;
}