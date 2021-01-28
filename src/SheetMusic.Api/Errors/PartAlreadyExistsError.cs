using System.Net;

namespace SheetMusic.Api.Errors
{
    public class PartAlreadyExistsError : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;

        public PartAlreadyExistsError(string partName) : base($"Part '{partName}' already exists")
        {
        }
    }
}
