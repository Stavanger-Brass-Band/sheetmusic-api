using SheetMusic.Api.Database.Entities;
using System;

namespace SheetMusic.Api.Sets.ViewModels;

public class ApiCategory
{
    public ApiCategory()
    {
    }

    public ApiCategory(Category category)
    {
        Id = category.Id;
        Name = category.Name;
        Inactive = category.Inactive;
    }

    public Guid Id { get; set; }

    public string? Name { get; set; }

    public bool Inactive { get; set; }
}
