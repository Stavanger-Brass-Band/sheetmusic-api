using System;

namespace SheetMusic.Api.Projects;

public class NewProjectRequest
{
    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Comments { get; set; }
}
