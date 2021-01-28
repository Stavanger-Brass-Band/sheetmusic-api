using System;
using System.Net;

namespace SheetMusic.Api.Errors
{
    public class ExceptionBase : Exception
    {
        public ExceptionBase(string message) : base(message)
        {
        }

        public ExceptionBase(string message, Exception innerException) : base(message, innerException)
        {
        }

        public virtual HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;
    }
}
