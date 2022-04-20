using System;

namespace SheetMusic.Api.Test.Models;

public class PutSetModel
{
    public Guid OriginatingId { get; set; }
    public string Title { get; set; } = null!;
    public string? Composer { get; set; } = null!;
    public string? RecordingUrl { get; set; }
    public string? Arranger { get; set; } = null!;
    public string? SoleSellingAgent { get; set; } = null!;
    public string? MissingParts { get; set; } = null!;
    public string? BorrowedFrom { get; set; }

}
