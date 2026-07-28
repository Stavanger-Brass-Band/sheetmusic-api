using System.Text.Json;

namespace SheetMusic.ImportCli;

/// <summary>
/// Shared System.Text.Json options for the import CLI. Uses the "Web" defaults
/// (camelCase, case-insensitive matching) to mirror ASP.NET Core's default controller
/// serialization behavior when reading API responses back into PascalCase DTOs.
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
