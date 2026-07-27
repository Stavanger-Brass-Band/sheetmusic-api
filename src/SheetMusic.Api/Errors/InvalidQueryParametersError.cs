using System;
using System.Net;

namespace SheetMusic.Api.Errors;

/// <summary>
/// Raised when consumer supplied query parameters (OData or otherwise) cannot be understood.
/// Surfaces as a 400 Bad Request with a ProblemDetails body instead of a bare 500.
/// </summary>
public class InvalidQueryParametersError : ExceptionBase
{
    public InvalidQueryParametersError(string message) : base(message)
    {
    }

    public InvalidQueryParametersError(string message, Exception innerException) : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
