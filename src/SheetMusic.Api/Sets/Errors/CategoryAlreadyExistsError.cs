using SheetMusic.Api.Errors;
using System.Net;

namespace SheetMusic.Api.Sets.Errors;

public class CategoryAlreadyExistsError(string categoryName) : ExceptionBase($"Category '{categoryName}' already exists")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
