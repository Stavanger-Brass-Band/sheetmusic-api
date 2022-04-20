using System;

namespace SheetMusic.Api.Database.Entities;

public class SheetMusicCategory
{
    public Guid Id { get; set; }
    public Guid SheetMusicId { get; set; }
    public Guid CategoryId { get; set; }
    public SheetMusicSet SheetMusicSet { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
