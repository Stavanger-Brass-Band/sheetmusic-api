using System;

namespace SheetMusic.Api.Database.Entities;

public class SheetMusicCategory
{
    public Guid Id { get; set; }
    public Guid SheetMusicSetId { get; set; }
    public Guid CategoryId { get; set; }
    public string Source { get; set; } = "Human";
    public string? ModelVersion { get; set; }
    public string? PromptVersion { get; set; }
    public DateTimeOffset? SuggestedAt { get; set; }
    public SheetMusicSet SheetMusicSet { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
