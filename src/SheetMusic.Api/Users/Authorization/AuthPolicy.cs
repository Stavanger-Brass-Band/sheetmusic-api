namespace SheetMusic.Api.Users.Authorization;

/// <summary>
/// Names of the authorization policies registered in <c>AddSheetMusicAuthentication</c>. Kept separate
/// from the role names in <see cref="Roles"/> so the set of roles satisfying a policy is defined in one
/// place instead of being duplicated across every <c>[Authorize]</c> attribute.
/// </summary>
public static class AuthPolicy
{
    /// <summary>
    /// Requires the <see cref="Roles.Admin"/> role. Used for user administration and rebuilding the part index.
    /// </summary>
    public const string Admin = "AdminOnly";

    /// <summary>
    /// Requires <see cref="Roles.Admin"/> or <see cref="Roles.Noteansvarlig"/>. Used for every write
    /// operation on the music catalogue: sets, parts and categories.
    /// </summary>
    public const string ManageMusic = "ManageMusic";

    /// <summary>
    /// Requires <see cref="Roles.Admin"/>, <see cref="Roles.Noteansvarlig"/> or <see cref="Roles.Prosjektleder"/>.
    /// Used for every write operation on Projects.
    /// </summary>
    public const string ManageProjects = "ManageProjects";
}
