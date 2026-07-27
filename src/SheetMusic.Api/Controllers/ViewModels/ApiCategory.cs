using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Controllers.ViewModels;

public class ApiCategory(Category category)
{
    public Guid Id { get; set; } = category.Id;

    public string? Name { get; set; } = category.Name;

    public bool Inactive { get; set; } = category.Inactive;
}
