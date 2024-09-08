using System.Net;

namespace SheetMusic.Api.Errors;

public class UserAlreadyExistsError(string email) : ExceptionBase($"User with email {email} already exists")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
