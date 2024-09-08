using System;

namespace SheetMusic.Api.BlobStorage;

public class PartRelatedToSet(Guid setId, Guid partId)
{
    public Guid SetId { get; set; } = setId;

    public Guid PartId { get; set; } = partId;

    public string BlobPath => $"{SetId}/{PartId}.pdf";
}
