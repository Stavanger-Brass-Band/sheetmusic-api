using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Controllers.ViewModels
{
    public class ApiPart
    {
        public ApiPart(MusicPart part)
        {
            Id = part.Id;
            Name = part.Name;
            SortOrder = part.SortOrder;
            Indexable = part.Indexable;
            Aliases = part.Aliases?.Where(a => a.Enabled).Select(a => a.Alias).ToList() ?? new List<string>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public int SortOrder { get; set; }

        public bool Indexable { get; set; }

        public List<string> Aliases { get; set; }
    }
}
