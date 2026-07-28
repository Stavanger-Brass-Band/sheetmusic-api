using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

public class CategoryAlreadyAssignedError(string setTitle, string categoryName) : ExceptionBase($"Category '{categoryName}' is already assigned to set '{setTitle}'")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
