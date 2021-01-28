using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities
{
    public class Category
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public bool Inactive { get; set; }

        public List<SheetMusicCategory> SheetMusicCategories { get; set; } = null!;
    }
}
