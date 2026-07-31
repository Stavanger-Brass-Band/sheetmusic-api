using Microsoft.AspNetCore.Identity;
using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Authorization;

/// <summary>
/// Resolves the <see cref="ApplicationUser"/> behind a JWT "sub"/Name claim.
///
/// The resolved identity carries its role names, so authentication can enrich the principal with
/// role claims without a second round-trip. Roles are resolved per request rather than minted into
/// the token, so a role change takes effect immediately without re-issuing a token.
/// </summary>
public class AuthResolver(UserManager<ApplicationUser> userManager)
{
    /// <summary>
    /// Resolves the <see cref="ApplicationUser"/> matching <paramref name="claimUserId"/> and its
    /// current roles, or <c>null</c> if no such user exists.
    /// </summary>
    public async Task<ResolvedIdentity?> ResolveAsync(Guid claimUserId)
    {
        var user = await userManager.FindByIdAsync(claimUserId.ToString());
        if (user == null)
            return null;

        return new ResolvedIdentity(user, [.. await userManager.GetRolesAsync(user)]);
    }
}

/// <summary>An <see cref="ApplicationUser"/> together with its currently assigned role names.</summary>
public record ResolvedIdentity(ApplicationUser User, IReadOnlyList<string> Roles)
{
    /// <summary>Whether the resolved user is currently inactive and should be denied access.</summary>
    public bool IsInactive => User.Inactive;
}

