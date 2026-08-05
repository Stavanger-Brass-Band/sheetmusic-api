using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public class UnableToIdentifyUserError() : ExceptionBase("Unable to find Name claim and identify user")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}