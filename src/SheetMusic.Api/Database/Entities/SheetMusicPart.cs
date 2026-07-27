using System;

namespace SheetMusic.Api.Database.Entities;

public class SheetMusicPart
{
    public Guid Id { get; set; }
    public Guid MusicPartId { get; set; }
    public Guid SetId { get; set; }

    public SheetMusicSet Set { get; set; } = null!;
    public MusicPart Part { get; set; } = null!;
}
