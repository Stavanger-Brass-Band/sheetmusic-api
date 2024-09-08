using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities;

public class SheetMusicSet(int archiveNumber, string title)
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int ArchiveNumber { get; set; } = archiveNumber;
    public string Title { get; set; } = title;
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
    public string? SoleSellingAgent { get; set; }
    public string? MissingParts { get; set; }
    public string? RecordingUrl { get; internal set; }
    public string? BorrowedFrom { get; set; }
    public DateTimeOffset? BorrowedDateTime { get; set; }

    public List<SheetMusicPart> Parts { get; set; } = new List<SheetMusicPart>();
    public List<SheetMusicCategory> Categories { get; set; } = new List<SheetMusicCategory>();
    public List<ProjectSheetMusicSet> ProjectConnections { get; set; } = new List<ProjectSheetMusicSet>();

}
