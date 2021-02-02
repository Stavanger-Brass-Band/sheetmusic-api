using System;

namespace SheetMusic.Api.Test.Models
{
    public class ApiSetPart
    {
        public Guid RelationshipId { get; set; }
        public Guid SetId { get; set; }
        public Guid MusicPartId { get; set; }
        public string Name { get; set; } = null!;
        public string? Aliases { get; set; }
        public string? PdfDownloadUrl { get; set; }
        public string? DeletePartUrl { get; set; }
    }
}
