using System;
using System.Net;

namespace SheetMusic.Api.Errors;

public class BlobInteractionError(string message, Exception innerException) : ExceptionBase(message, innerException)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.FailedDependency;
}
