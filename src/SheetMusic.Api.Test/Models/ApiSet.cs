using System;
using System.Collections.Generic;

namespace SheetMusic.Api.Test.Models;

public class ApiSet
{
    /// <summary>
    /// Correlation ID between request and response items
    /// </summary>
    public Guid OriginatingId { get; set; }
    public Guid Id { get; set; }
    public int ArchiveNumber { get; set; }
    public string? Title { get; set; }
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
    public string? SoleSellingAgent { get; set; }
    public string? MissingParts { get; set; }
    public string? RecordingUrl { get; set; }
    public bool HasBeenScanned { get; set; }
    public bool Borrowed { get; set; }
    public string? BorrowedFrom { get; set; }
    public DateTimeOffset? BorrowedDateTime { get; set; }
    public string ZipDownloadUrl { get; set; } = null!;
    public string PartsUrl { get; set; } = null!;
    public List<ApiSetPart>? Parts { get; set; }
}
