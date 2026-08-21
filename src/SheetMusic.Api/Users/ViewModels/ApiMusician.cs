using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Users.ViewModels;

/// <summary>
/// The privacy-reduced musician information available to authenticated members.
/// </summary>
public class ApiMusician
{
    public ApiMusician(Musician musician, IReadOnlyList<string> roles)
    {
        ArgumentNullException.ThrowIfNull(musician);
        ArgumentNullException.ThrowIfNull(roles);
        var user = musician.ApplicationUser ?? throw new ArgumentException("A musician must have an associated user", nameof(musician));

        Id = user.Id;
        Name = user.DisplayName ?? musician.Name ?? string.Empty;
        ProfilePicture = user.ProfilePictureVersion is { } version ? new ApiProfilePicture(version) : null;
        Parts = musician.MusicianMusicParts
            .OrderBy(musicianPart => musicianPart.MusicPart.SortOrder)
            .Select(musicianPart => new ApiPart(musicianPart.MusicPart))
            .ToList();
        Roles = roles;
    }

    /// <summary>The musician's identifier.</summary>
    public Guid Id { get; }

    /// <summary>The musician's display name.</summary>
    public string Name { get; }

    /// <summary>The current profile-picture version, when a picture exists.</summary>
    public ApiProfilePicture? ProfilePicture { get; }

    /// <summary>The instrument parts assigned to the musician.</summary>
    public IReadOnlyList<ApiPart> Parts { get; }

    /// <summary>The roles assigned to the musician's user account.</summary>
    public IReadOnlyList<string> Roles { get; }
}