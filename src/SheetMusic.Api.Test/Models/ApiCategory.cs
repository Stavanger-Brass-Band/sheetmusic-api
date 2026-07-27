using System;

namespace SheetMusic.Api.Test.Models;

public class ApiCategory
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public bool Inactive { get; set; }
}
