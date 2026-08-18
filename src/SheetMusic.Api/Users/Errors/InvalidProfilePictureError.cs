using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public sealed class InvalidProfilePictureError(string message) : ExceptionBase(message)
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}