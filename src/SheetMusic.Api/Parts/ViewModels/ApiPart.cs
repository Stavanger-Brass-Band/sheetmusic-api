using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Parts.ViewModels;

public class ApiPart
{
    /// <summary>
    /// Parameterless constructor required for JSON deserialization (e.g. in integration tests).
    /// </summary>
    public ApiPart()
    {
    }

    public ApiPart(MusicPart part)
    {
        Id = part.Id;
        Name = part.Name;
        SortOrder = part.SortOrder;
        Indexable = part.Indexable;
        InstrumentGroup = part.InstrumentGroup;
        Aliases = part.Aliases?.Where(a => a.Enabled).Select(a => a.Alias).ToList() ?? new List<string>();
    }

    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool Indexable { get; set; }

    public InstrumentGroup? InstrumentGroup { get; set; }

    public List<string> Aliases { get; set; } = new List<string>();
}
