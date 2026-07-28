using System;

namespace SheetMusic.Api.Sets;

public class PartRelatedToSet(Guid setId, Guid partId)
{
    public Guid SetId { get; set; } = setId;

    public Guid PartId { get; set; } = partId;

    public string BlobPath => $"{SetId}/{PartId}.pdf";
}
