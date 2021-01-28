using System.Net;

namespace SheetMusic.Api.Errors
{
    public class ArchiveNumberOccupiedError : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;

        public ArchiveNumberOccupiedError(int archiveNumber) : base($"Archive number [{archiveNumber}] is already in use")
        {
        }
    }
}
