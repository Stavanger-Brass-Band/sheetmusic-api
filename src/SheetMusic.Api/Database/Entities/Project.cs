using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities;

public class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Comments { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public List<ProjectSheetMusicSet> SetConnections { get; set; } = null!;
}
