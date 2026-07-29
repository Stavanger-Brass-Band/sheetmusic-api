using Microsoft.AspNetCore.Identity;

namespace SheetMusic.Api.Users.ViewModels;

/// <summary>
/// The password complexity policy enforced by ASP.NET Core Identity, so API consumers can render a
/// requirements checklist up front and interpret <see cref="Errors.PasswordRequirementsNotMetError"/>
/// responses without hardcoding the rules. Always derived from the configured
/// <see cref="PasswordOptions"/>, never hardcoded, so the advertised policy cannot drift from the
/// enforced one.
/// </summary>
public class ApiPasswordRequirements
{
    public int MinimumLength { get; set; }
    public bool RequireDigit { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireNonAlphanumeric { get; set; }
    public int RequiredUniqueChars { get; set; }

    public static ApiPasswordRequirements FromPasswordOptions(PasswordOptions options) => new()
    {
        MinimumLength = options.RequiredLength,
        RequireDigit = options.RequireDigit,
        RequireUppercase = options.RequireUppercase,
        RequireLowercase = options.RequireLowercase,
        RequireNonAlphanumeric = options.RequireNonAlphanumeric,
        RequiredUniqueChars = options.RequiredUniqueChars
    };
}
