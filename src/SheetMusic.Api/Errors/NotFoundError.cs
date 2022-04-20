using System;
using System.Net;

namespace SheetMusic.Api.Errors;

public class NotFoundError : ExceptionBase
{
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;

    public NotFoundError(string resource, string message = "Resource not found") : base($"{message}: {resource}")
    {
    }
}
