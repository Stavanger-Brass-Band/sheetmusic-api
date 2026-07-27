using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public class InvalidPasswordResetTokenError() : ExceptionBase("Password reset token is invalid or has expired.")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
