using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Controllers.ViewModels;

public class ApiProject(Project project)
{
    public Guid Id { get; set; } = project.Id;

    public string Name { get; set; } = project.Name;

    public string? Comments { get; set; } = project.Comments;

    public DateTime StartDate { get; set; } = project.StartDate;

    public DateTime EndDate { get; set; } = project.EndDate;
}
