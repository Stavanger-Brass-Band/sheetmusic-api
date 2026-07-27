using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SheetMusic.Api.Database.Entities;

public class Musician
{
    public Guid Id { get; set; }
    public string? Name { get; set; }

    [Obsolete("Use ApplicationUser.Email instead")]
    public string? Email { get; set; }

    [Obsolete("Use ApplicationUser for authentication")]
    public byte[]? PasswordHash { get; set; }

    [Obsolete("Use ApplicationUser for authentication")]
    public byte[]? PasswordSalt { get; set; }

    [Obsolete("Use ApplicationUser.Inactive instead")]
    public bool Inactive { get; set; }

    public List<MusicianMusicPart> MusicianMusicParts { get; set; } = null!;

    [Obsolete("Use ApplicationUser roles instead")]
    public Guid UserGroupId { get; set; }

    [Obsolete("Use ApplicationUser roles instead")]
    public UserGroup UserGroup { get; set; } = null!;

    public Guid? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
}
