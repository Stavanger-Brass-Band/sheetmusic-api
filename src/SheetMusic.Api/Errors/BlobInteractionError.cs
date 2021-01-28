using System;
using System.Net;

namespace SheetMusic.Api.Errors
{
    public class BlobInteractionError : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.FailedDependency;

        public BlobInteractionError(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
