using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Parts.Entities;

public class MusicPart
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool Indexable { get; set; }

    public List<SheetMusicPart> Parts { get; set; } = null!;
    public List<MusicianMusicPart> MusicianMusicParts { get; set; } = null!;
    public List<MusicPartAlias> Aliases { get; set; } = null!;
}

