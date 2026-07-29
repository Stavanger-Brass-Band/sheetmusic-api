using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

/// <summary>
/// The provided username/password combination did not authenticate, the user does not exist, or the
/// user is inactive. Deliberately a single generic error for all three cases - distinguishing them in
/// the response would let a caller enumerate registered accounts or account state.
/// </summary>
public class InvalidCredentialsError() : ExceptionBase("Username or password is incorrect")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
