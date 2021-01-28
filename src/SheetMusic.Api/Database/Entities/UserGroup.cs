using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Database.Entities
{
    public class UserGroup
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public List<Musician> Musicians { get; set; } = new List<Musician>();
    }
}
