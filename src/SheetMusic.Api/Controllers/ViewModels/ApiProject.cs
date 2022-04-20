using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Controllers.ViewModels;

public class ApiProject
{


    public ApiProject(Project project)
    {
        Id = project.Id;
        Name = project.Name;
        Comments = project.Comments;
        StartDate = project.StartDate;
        EndDate = project.EndDate;
    }

    public Guid Id { get; set; }

    public string Name { get; set; }

    public string? Comments { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}
