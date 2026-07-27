using System.Collections.Generic;
using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Users.Errors;

public class IdentityOperationError(IEnumerable<string> errors) : ExceptionBase($"Identity operation failed: {string.Join("; ", errors)}")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
