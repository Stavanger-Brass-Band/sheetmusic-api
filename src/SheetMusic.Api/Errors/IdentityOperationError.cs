using System.Collections.Generic;
using System.Net;

namespace SheetMusic.Api.Errors;

public class IdentityOperationError(IEnumerable<string> errors) : ExceptionBase($"Identity operation failed: {string.Join("; ", errors)}")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
}
