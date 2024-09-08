using System;
using System.Net;

namespace SheetMusic.Api.Errors;

public class NotFoundError(string resource, string message = "Resource not found") : ExceptionBase($"{message}: {resource}")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}
