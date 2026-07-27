using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Projects.ViewModels;

public class ApiProject
{
    /// <summary>
    /// Parameterless constructor required for JSON deserialization (e.g. in integration tests).
    /// </summary>
    public ApiProject()
    {
    }

    public ApiProject(Project project)
    {
        Id = project.Id;
        Name = project.Name;
        Comments = project.Comments;
        StartDate = project.StartDate;
        EndDate = project.EndDate;
    }

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Comments { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
