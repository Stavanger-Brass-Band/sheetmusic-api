using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities;

public class Musician
{
    public Guid Id { get; set; }
    public string? Name { get; set; }

    public List<MusicianMusicPart> MusicianMusicParts { get; set; } = null!;

    public Guid? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
}
