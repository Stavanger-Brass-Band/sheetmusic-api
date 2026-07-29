using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

/// <summary>
/// The supplied refresh token does not exist, has already been used/revoked, or has expired.
/// </summary>
public class InvalidRefreshTokenError() : ExceptionBase("Refresh token is invalid, expired, or has already been used")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
