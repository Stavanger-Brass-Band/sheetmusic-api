using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Sets.ViewModels;

/// <summary>
/// A project summary included in an expanded sheet music set.
/// </summary>
public class ApiProjectSummary
{
    public ApiProjectSummary()
    {
    }

    public ApiProjectSummary(Project project)
    {
        Id = project.Id;
        Name = project.Name;
    }

    /// <summary>
    /// Identifier in the database.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Project name.
    /// </summary>
    public string Name { get; set; } = null!;
}
