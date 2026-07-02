using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using System;
using System.Threading.Tasks;

namespace SheetMusic.Api.Authorization;

/// <summary>
/// Resolves the identity behind a JWT "sub"/Name claim, which can be either an
/// <see cref="ApplicationUser"/> ID (v2 tokens) or a legacy <see cref="Musician"/> ID (v1 tokens).
///
/// Legacy musicians may or may not be linked to an <see cref="ApplicationUser"/> via
/// <see cref="Musician.ApplicationUserId"/>. Musicians that predate the Identity migration are
/// never linked, so callers must be able to make authn/authz decisions directly off the legacy
/// <see cref="Musician"/> record in that case.
/// </summary>
public class LegacyAuthResolver(UserManager<ApplicationUser> userManager, SheetMusicContext dbContext)
{
    public async Task<ResolvedIdentity?> ResolveAsync(Guid claimUserId)
    {
        // Try as ApplicationUser ID first (v2 tokens)
        var user = await userManager.FindByIdAsync(claimUserId.ToString());
        if (user != null)
            return new ResolvedIdentity(user, null);

        // Fall back to Musician ID lookup (v1 tokens)
#pragma warning disable CS0612 // Type or member is obsolete
        var musician = await dbContext.Musicians.Include(m => m.UserGroup).FirstOrDefaultAsync(m => m.Id == claimUserId);
#pragma warning restore CS0612

        if (musician == null)
            return null;

        // Legacy musician linked to an ApplicationUser
        if (musician.ApplicationUserId != null)
        {
            user = await userManager.FindByIdAsync(musician.ApplicationUserId.Value.ToString());
            return user != null ? new ResolvedIdentity(user, null) : null;
        }

        // Legacy musician never linked to an ApplicationUser
        return new ResolvedIdentity(null, musician);
    }
}

public record ResolvedIdentity(ApplicationUser? User, Musician? LegacyMusician)
{
#pragma warning disable CS0612 // Type or member is obsolete
    public bool IsInactive => User?.Inactive ?? LegacyMusician?.Inactive ?? true;
#pragma warning restore CS0612
}
