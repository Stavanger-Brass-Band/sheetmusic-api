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

    /// <summary>
    /// Position of the set within the project, lower values sort first
    /// </summary>
    public int SortOrder { get; set; }

    public SheetMusicSet Set { get; set; } = null!;

    public Project Project { get; set; } = null!;
}
