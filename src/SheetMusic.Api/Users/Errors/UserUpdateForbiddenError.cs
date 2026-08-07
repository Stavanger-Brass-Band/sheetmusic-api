using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public class UserUpdateForbiddenError() : ExceptionBase("Only the user themselves or an Administrator can update the user")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}