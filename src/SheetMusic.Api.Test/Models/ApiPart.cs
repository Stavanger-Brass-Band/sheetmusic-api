using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Test.Models;

public class ApiPart
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool Indexable { get; set; }

    public List<string> Aliases { get; set; } = new List<string>();
}
