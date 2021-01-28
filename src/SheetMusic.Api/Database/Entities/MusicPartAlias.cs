using System;

namespace SheetMusic.Api.Database.Entities
{
    public class MusicPartAlias
    {
        public Guid Id { get; set; }

        public string Alias { get; set; } = null!;

        public bool Enabled { get; set; }

        public Guid MusicPartId { get; set; }

        public MusicPart MusicPart { get; set; } = null!;

    }
}
