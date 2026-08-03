using System;

namespace SheetMusic.Api.Database.Entities;

public class SheetMusicPart
{
    public Guid Id { get; set; }
    public Guid MusicPartId { get; set; }
    public Guid SetId { get; set; }
    public string Source { get; set; } = "Human";
    public string? ModelVersion { get; set; }
    public string? PromptVersion { get; set; }
    public DateTimeOffset? SuggestedAt { get; set; }

    public SheetMusicSet Set { get; set; } = null!;
    public MusicPart Part { get; set; } = null!;
}
