using System.Net;

namespace SheetMusic.Api.Errors
{
    public class UserAlreadyExistsError : ExceptionBase
    {
        public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;

        public UserAlreadyExistsError(string email) : base($"User with email {email} already exists")
        {
        }
    }
}
