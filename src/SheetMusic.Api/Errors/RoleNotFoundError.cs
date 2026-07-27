using System.Net;

namespace SheetMusic.Api.Errors;

public class RoleNotFoundError(string roleName) : ExceptionBase($"Role '{roleName}' does not exist")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}
