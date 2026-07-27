using System.Net;

namespace SheetMusic.Api.Errors;

public class CategoryAlreadyExistsError(string categoryName) : ExceptionBase($"Category '{categoryName}' already exists")
{
    public override HttpStatusCode StatusCode => HttpStatusCode.Conflict;
}
