using SheetMusic.Api.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Controllers.ViewModels
{
    public class ApiSet
    {
        public ApiSet()
        {

        }

        public ApiSet(SheetMusicSet set)
        {
            Id = set.Id;
            ArchiveNumber = set.ArchiveNumber;
            Title = set.Title;
            Composer = set.Composer;
            Arranger = set.Arranger;
            SoleSellingAgent = set.SoleSellingAgent;
            MissingParts = set.MissingParts;
            RecordingUrl = set.RecordingUrl;
            HasBeenScanned = set.Parts?.Any() ?? false;
            BorrowedFrom = set.BorrowedFrom;
            BorrowedDateTime = set.BorrowedDateTime;
        }

        /// <summary>
        /// Identifier in DB
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Number in physical archive
        /// </summary>
        public int ArchiveNumber { get; set; }

        public string? Title { get; set; }
        public string? Composer { get; set; }
        public string? Arranger { get; set; }
        public string? SoleSellingAgent { get; set; }
        public string? MissingParts { get; set; }
        public string? RecordingUrl { get; set; }
        public bool HasBeenScanned { get; set; }

        public bool Borrowed => BorrowedFrom != null;
        public string? BorrowedFrom { get; set; }
        public DateTimeOffset? BorrowedDateTime { get; set; }

        /// <summary>
        /// Download pdf of parts for set on this URL
        /// </summary>
        public string ZipDownloadUrl { get; set; } = null!;

        /// <summary>
        /// List parts of set on this URL
        /// </summary>
        public string PartsUrl { get; set; } = null!;

        /// <summary>
        /// A list of parts for the set, if included
        /// </summary>
        public List<ApiSheetMusicPart>? Parts { get; set; }
    }
}
