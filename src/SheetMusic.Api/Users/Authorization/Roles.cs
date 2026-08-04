namespace SheetMusic.Api.Users.Authorization;

/// <summary>
/// The role names persisted in Identity (<c>AspNetRoles.Name</c>) and exposed on the wire through
/// <c>ApiUserDetail.Roles</c> and <c>PUT users/{id}/roles</c>.
///
/// Role names are a separate concept from the policy names in <see cref="AuthPolicy"/>: a role says
/// what a user <em>is</em>, a policy says what an endpoint <em>requires</em>.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Full control, including user administration and rebuilding the part index.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Librarian. Full control over the music catalogue (sets, parts, projects, categories),
    /// but no control over users.
    /// </summary>
    public const string Noteansvarlig = "Noteansvarlig";

    /// <summary>
    /// Ordinary band member. Read-only access to the music catalogue.
    /// </summary>
    public const string Musikant = "Musikant";

    /// <summary>
    /// Library reader. Read-only access to the entire music catalogue, including sets outside active projects.
    /// </summary>
    public const string Arkivleser = "Arkivleser";

    /// <summary>
    /// Project manager. Can create, update and delete Projects and manage which sets are assigned to them,
    /// but has no edit rights over Sets, Parts or Categories.
    /// </summary>
    public const string Prosjektleder = "Prosjektleder";

    /// <summary>
    /// All role names that can be assigned to a user.
    /// </summary>
    public static readonly string[] All = [Admin, Noteansvarlig, Musikant, Arkivleser, Prosjektleder];
}
