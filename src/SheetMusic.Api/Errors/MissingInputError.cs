using System.Net;

namespace SheetMusic.Api.Errors;

public class MissingInputError(string fieldName) : ExceptionBase($"{fieldName} must have a value")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
