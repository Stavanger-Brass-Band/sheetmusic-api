using System;

namespace SheetMusic.Api.Controllers.RequestModels;

public class UpdateProjectRequest
{
    public string Name { get; set; } = null!;

    public string? Comments { get; set; } 

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
