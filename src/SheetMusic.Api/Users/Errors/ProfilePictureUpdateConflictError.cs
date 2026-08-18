using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public sealed class ProfilePictureUpdateConflictError() : ExceptionBase("The profile picture was changed by another request. Retry the operation.")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}