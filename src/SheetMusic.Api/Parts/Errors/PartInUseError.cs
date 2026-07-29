using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Parts.Errors;

public class PartInUseError(string partName) : ExceptionBase($"Part '{partName}' is linked to musicians or sets and cannot be deleted")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
