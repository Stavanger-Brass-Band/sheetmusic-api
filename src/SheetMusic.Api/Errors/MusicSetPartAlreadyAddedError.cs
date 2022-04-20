using System;
using System.Net;

namespace SheetMusic.Api.Errors;

public class MusicSetPartAlreadyAddedError : ExceptionBase
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
        
    public MusicSetPartAlreadyAddedError(string setTitle, string partName) : base($"Part {partName} already added for set {setTitle}")
    {
    }
}
