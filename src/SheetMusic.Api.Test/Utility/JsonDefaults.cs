using System.Text.Json;

namespace SheetMusic.Api.Test.Utility;

/// <summary>
/// Shared System.Text.Json options for the test project. Uses the "Web" defaults
/// (camelCase, case-insensitive matching) to mirror ASP.NET Core's default controller
/// serialization behavior when reading responses back into PascalCase test DTOs.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
