using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Users.Authorization;

/// <summary>
/// Resolves the catalogue resources available to the current user.
/// </summary>
public class CatalogAccessService(SheetMusicContext db, IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Gets whether the current user can access the specified set.</summary>
    public async Task<bool> CanAccessSetAsync(Guid setId, CancellationToken cancellationToken = default)
    {
        if (HasFullLibraryAccess())
            return true;

        if (!IsMusikant())
            return false;

        var now = DateTime.UtcNow;
        return await db.ProjectSheetMusicSets.AnyAsync(connection =>
            connection.SheetMusicSetId == setId &&
            connection.Project.StartDate <= now &&
            connection.Project.EndDate >= now,
            cancellationToken);
    }

    /// <summary>Gets whether the current user can access the specified project.</summary>
    public bool CanAccessProject(DateTime startDate, DateTime endDate) =>
        HasFullLibraryAccess() || (IsMusikant() && startDate <= DateTime.UtcNow && endDate >= DateTime.UtcNow);

    /// <summary>Filters set identifiers to the catalogue resources visible to the current user.</summary>
    public async Task<HashSet<Guid>> GetAccessibleSetIdsAsync(IEnumerable<Guid> setIds, CancellationToken cancellationToken = default)
    {
        var ids = setIds.ToList();
        if (HasFullLibraryAccess())
            return [.. ids];

        if (!IsMusikant())
            return [];

        var now = DateTime.UtcNow;
        return [.. await db.ProjectSheetMusicSets
            .Where(connection => ids.Contains(connection.SheetMusicSetId) &&
                connection.Project.StartDate <= now && connection.Project.EndDate >= now)
            .Select(connection => connection.SheetMusicSetId)
            .ToListAsync(cancellationToken)];
    }

    private bool HasFullLibraryAccess() => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Noteansvarlig) || User.IsInRole(Roles.Arkivleser);

    private bool IsMusikant() => User.IsInRole(Roles.Musikant);

    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
}
