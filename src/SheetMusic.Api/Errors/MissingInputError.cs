using System.Net;

namespace SheetMusic.Api.Errors
{
    public class MissingInputError : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

        public MissingInputError(string fieldName) : base($"{fieldName} must have a value")
        {
        }
    }
}
