using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using SheetMusic.Api.Parts;

namespace SheetMusic.Api.Database.Entities;

public class MusicPart
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool Indexable { get; set; }
    [Column(TypeName = "varchar(50)")]
    public InstrumentGroup? InstrumentGroup { get; set; }

    public List<SheetMusicPart> Parts { get; set; } = null!;
    public List<MusicianMusicPart> MusicianMusicParts { get; set; } = null!;
    public List<MusicPartAlias> Aliases { get; set; } = null!;
}