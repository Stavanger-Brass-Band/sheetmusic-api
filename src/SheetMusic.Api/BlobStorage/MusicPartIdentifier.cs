using System;

namespace SheetMusic.Api.BlobStorage
{
    public class MusicPartIdentifier
    {
        public MusicPartIdentifier(Guid setId, Guid partId)
        {
            SetId = setId;
            PartId = partId;
        }

        public Guid SetId { get; set; }

        public Guid PartId { get; set; }

        public string BlobPath => $"{SetId}/{PartId}.pdf";
    }
}
