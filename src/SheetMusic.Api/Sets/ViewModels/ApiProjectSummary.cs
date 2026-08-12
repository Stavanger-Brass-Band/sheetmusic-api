using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Sets.ViewModels;

/// <summary>
/// A project summary included in an expanded sheet music set.
/// </summary>
public class ApiProjectSummary(Project project)
{
    /// <summary>
    /// Identifier in the database.
    /// </summary>
    public Guid Id { get; } = project.Id;

    /// <summary>
    /// Project name.
    /// </summary>
    public string Name { get; } = project.Name;
}