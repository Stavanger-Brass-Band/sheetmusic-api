using System.Net;

namespace SheetMusic.Api.Errors;

public class ArchiveNumberOccupiedError(int archiveNumber) : ExceptionBase($"Archive number [{archiveNumber}] is already in use")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
