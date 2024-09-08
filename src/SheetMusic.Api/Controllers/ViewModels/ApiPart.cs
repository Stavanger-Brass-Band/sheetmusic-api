using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Controllers.ViewModels;

public class ApiPart(MusicPart part)
{
    public Guid Id { get; set; } = part.Id;

    public string Name { get; set; } = part.Name;

    public int SortOrder { get; set; } = part.SortOrder;

    public bool Indexable { get; set; } = part.Indexable;

    public List<string> Aliases { get; set; } = part.Aliases?.Where(a => a.Enabled).Select(a => a.Alias).ToList() ?? new List<string>();
}
