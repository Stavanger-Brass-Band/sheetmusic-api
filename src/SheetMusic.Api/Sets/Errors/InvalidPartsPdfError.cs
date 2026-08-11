using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

/// <summary>
/// Thrown when an uploaded combined score cannot be opened as a PDF.
/// </summary>
public sealed class InvalidPartsPdfError(Exception innerException) : ExceptionBase("The uploaded file is not a valid PDF.", innerException)
{
    /// <inheritdoc />
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}