using System;

namespace SheetMusic.Api.Test.Models;

public class ApiProject
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Comments { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
