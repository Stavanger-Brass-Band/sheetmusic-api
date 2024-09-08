using System;
using System.Net;

namespace SheetMusic.Api.Errors;

public class MusicSetPartAlreadyAddedError(string setTitle, string partName) : ExceptionBase($"Part {partName} already added for set {setTitle}")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
