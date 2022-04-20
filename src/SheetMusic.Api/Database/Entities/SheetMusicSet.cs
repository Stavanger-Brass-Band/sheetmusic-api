using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities;

public class SheetMusicSet
{
    public SheetMusicSet(int archiveNumber, string title)
    {
        Id = Guid.NewGuid();
        ArchiveNumber = archiveNumber;
        Title = title;
        Parts = new List<SheetMusicPart>();
        Categories = new List<SheetMusicCategory>();
        ProjectConnections = new List<ProjectSheetMusicSet>();
    }

    public Guid Id { get; set; }
    public int ArchiveNumber { get; set; }
    public string Title { get; set; }
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
    public string? SoleSellingAgent { get; set; }
    public string? MissingParts { get; set; }
    public string? RecordingUrl { get; internal set; }
    public string? BorrowedFrom { get; set; }
    public DateTimeOffset? BorrowedDateTime { get; set; }

    public List<SheetMusicPart> Parts { get; set; }
    public List<SheetMusicCategory> Categories { get; set; }
    public List<ProjectSheetMusicSet> ProjectConnections { get; set; }

}
