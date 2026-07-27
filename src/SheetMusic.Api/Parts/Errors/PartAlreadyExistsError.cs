using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Parts.Errors;

public class PartAlreadyExistsError(string partName) : ExceptionBase($"Part '{partName}' already exists")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
