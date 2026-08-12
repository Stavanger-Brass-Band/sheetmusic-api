using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

/// <summary>Thrown when a set title cannot be extracted from a combined score PDF.</summary>
public sealed class PdfSetMetadataError() : ExceptionBase("A set title could not be extracted from the PDF.")
{
    /// <inheritdoc />
    public override HttpStatusCode StatusCode => HttpStatusCode.UnprocessableEntity;
}