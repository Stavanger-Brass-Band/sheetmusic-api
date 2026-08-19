using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
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
    private const string PartiturName = "Partitur";

    /// <summary>Gets the current catalogue user's identifier.</summary>
    public Guid? CurrentUserId => GetUserId();

    /// <summary>Filters sets to the catalogue resources visible to the current user.</summary>
    public IQueryable<SheetMusicSet> FilterAccessibleSets(IQueryable<SheetMusicSet> sets)
    {
        if (HasFullLibraryAccess())
            return sets;

        var userId = GetUserId();
        if (userId is null)
            return sets.Where(_ => false);

        var now = DateTime.UtcNow;
        var permittedParts = IsMusikant()
            ? FilterPartsForMusikant(db.SheetMusicParts, userId.Value)
            : db.SheetMusicParts.Where(_ => false);

        return sets.Where(set =>
            set.Parts.Any(part => part.Part.Name == PartiturName) ||
            set.ProjectConnections.Any(connection =>
                connection.Project.StartDate <= now && connection.Project.EndDate >= now) &&
            permittedParts.Any(part => part.SetId == set.Id));
    }

    /// <summary>Filters set parts to the catalogue resources visible to the current user.</summary>
    public IQueryable<SheetMusicPart> FilterAccessibleParts(IQueryable<SheetMusicPart> parts)
    {
        if (HasFullLibraryAccess())
            return parts;

        var userId = GetUserId();
        if (userId is null)
            return parts.Where(_ => false);

        if (!IsMusikant())
            return parts.Where(part => part.Part.Name == PartiturName);

        return parts.Where(part => part.Part.Name == PartiturName).Union(FilterPartsForMusikant(parts, userId.Value));
    }

    /// <summary>Gets whether the current user can access the specified set.</summary>
    public Task<bool> CanAccessSetAsync(Guid setId, CancellationToken cancellationToken = default) =>
        FilterAccessibleSets(db.SheetMusicSets).AnyAsync(set => set.Id == setId, cancellationToken);

    /// <summary>Gets whether the current user can access the specified part on a set.</summary>
    public async Task<bool> CanAccessPartAsync(Guid setId, Guid musicPartId, CancellationToken cancellationToken = default) =>
        await FilterAccessibleSets(db.SheetMusicSets).AnyAsync(set => set.Id == setId, cancellationToken) &&
        await FilterAccessibleParts(db.SheetMusicParts).AnyAsync(part =>
            part.SetId == setId && part.MusicPartId == musicPartId,
            cancellationToken);

    /// <summary>Gets whether a user can currently access the specified part on a set.</summary>
    public async Task<bool> CanUserAccessPartAsync(Guid userId, Guid setId, Guid musicPartId, CancellationToken cancellationToken = default)
    {
        if (await db.SheetMusicParts.AnyAsync(part =>
            part.SetId == setId && part.MusicPartId == musicPartId && part.Part.Name == PartiturName,
            cancellationToken))
        {
            return true;
        }

        var roles = db.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(db.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name);

        if (await roles.AnyAsync(role => role == Roles.Admin || role == Roles.Noteansvarlig || role == Roles.Arkivleser, cancellationToken))
            return true;

        if (!await roles.AnyAsync(role => role == Roles.Musikant, cancellationToken))
            return false;

        var now = DateTime.UtcNow;
        return await FilterPartsForMusikant(db.SheetMusicParts, userId).AnyAsync(part =>
            part.SetId == setId && part.MusicPartId == musicPartId &&
            part.Set.ProjectConnections.Any(connection =>
                connection.Project.StartDate <= now && connection.Project.EndDate >= now),
            cancellationToken);
    }

    /// <summary>Gets whether the current user can access the specified project.</summary>
    public bool CanAccessProject(DateTime startDate, DateTime endDate) =>
        HasFullLibraryAccess() || IsProsjektleder() || (IsMusikant() && startDate <= DateTime.UtcNow && endDate >= DateTime.UtcNow);

    /// <summary>Filters set identifiers to the catalogue resources visible to the current user.</summary>
    public async Task<HashSet<Guid>> GetAccessibleSetIdsAsync(IEnumerable<Guid> setIds, CancellationToken cancellationToken = default)
    {
        var ids = setIds.ToList();
        return [.. await FilterAccessibleSets(db.SheetMusicSets)
            .Where(set => ids.Contains(set.Id))
            .Select(set => set.Id)
            .ToListAsync(cancellationToken)];
    }

    /// <summary>Filters part relationship identifiers to the catalogue resources visible to the current user.</summary>
    public async Task<HashSet<Guid>> GetAccessiblePartIdsAsync(IEnumerable<Guid> setIds, CancellationToken cancellationToken = default)
    {
        var ids = setIds.ToList();
        return [.. await FilterAccessibleParts(db.SheetMusicParts)
            .Where(part => ids.Contains(part.SetId))
            .Select(part => part.Id)
            .ToListAsync(cancellationToken)];
    }

    private IQueryable<SheetMusicPart> FilterPartsForMusikant(IQueryable<SheetMusicPart> parts, Guid userId)
    {
        var assignments = db.Set<MusicianMusicPart>()
            .Where(assignment => assignment.Musician.ApplicationUserId == userId);

        return parts.Where(part =>
            part.Part.InstrumentGroup != null && assignments.Any(assignment =>
                assignment.MusicPart.InstrumentGroup != null &&
                assignment.MusicPart.InstrumentGroup == part.Part.InstrumentGroup) ||
            part.Part.InstrumentGroup == null && part.Part.Indexable && assignments.Any(assignment =>
                assignment.MusicPartId == part.MusicPartId));
    }

    private Guid? GetUserId() => Guid.TryParse(User.Identity?.Name, out var userId) ? userId : null;

    private bool HasFullLibraryAccess() => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Noteansvarlig) || User.IsInRole(Roles.Arkivleser);

    private bool IsProsjektleder() => User.IsInRole(Roles.Prosjektleder);

    private bool IsMusikant() => User.IsInRole(Roles.Musikant);

    private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
}
