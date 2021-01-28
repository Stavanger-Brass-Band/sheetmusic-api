using System;

namespace SheetMusic.Api.Database.Entities
{
    public class MusicianMusicPart
    {
        public Guid Id { get; set; }
        public Guid MusicianId { get; set; }
        public Guid MusicPartId { get; set; }
        public Musician Musician { get; set; } = null!;
        public MusicPart MusicPart { get; set; } = null!;
    }
}
