using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

public class CategoryInUseError(string categoryName) : ExceptionBase($"Category '{categoryName}' is assigned to one or more sets and cannot be deleted")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
