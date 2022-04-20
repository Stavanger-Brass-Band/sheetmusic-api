using System;

namespace SheetMusic.Api.Database.Entities;

/// <summary>
/// Contains the sheet music set to project connection
/// </summary>
public class ProjectSheetMusicSet
{
    public Guid Id { get; set; }

    public Guid SheetMusicSetId { get; set; }

    public Guid ProjectId { get; set; }

    public SheetMusicSet Set { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
