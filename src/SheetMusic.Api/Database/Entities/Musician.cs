using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities;

public class Musician
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public byte[]? PasswordHash { get; set; }
    public byte[]? PasswordSalt { get; set; }
    public bool Inactive { get; set; }
    public List<MusicianMusicPart> MusicianMusicParts { get; set; } = null!;
    public Guid UserGroupId { get; set; }
    public UserGroup UserGroup { get; set; } = null!;
}
