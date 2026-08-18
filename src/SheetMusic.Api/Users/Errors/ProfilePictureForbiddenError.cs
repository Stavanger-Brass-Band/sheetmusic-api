using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public sealed class ProfilePictureForbiddenError() : ExceptionBase("Only the user or an Administrator can modify this profile picture")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Forbidden;
}