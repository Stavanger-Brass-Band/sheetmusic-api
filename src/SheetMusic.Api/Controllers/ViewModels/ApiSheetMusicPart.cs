using SheetMusic.Api.Database.Entities;
using System;
using System.Linq;

namespace SheetMusic.Api.Controllers.ViewModels;

public class ApiSheetMusicPart
{
    public ApiSheetMusicPart(SheetMusicPart sheetMusicPart)
    {
        if (sheetMusicPart == null)
        {
            throw new ArgumentNullException(nameof(sheetMusicPart));
        }

        if (sheetMusicPart.Part == null)
        {
            throw new ArgumentNullException(nameof(sheetMusicPart.Part));
        }

        RelationshipId = sheetMusicPart.Id;
        SetId = sheetMusicPart.SetId;
        MusicPartId = sheetMusicPart.MusicPartId;
        Name = sheetMusicPart.Part.Name;

        if (sheetMusicPart.Part.Aliases != null)
        {
            var aliasList = sheetMusicPart.Part.Aliases.Select(a => a.Alias);
            Aliases = string.Join(",", aliasList);
        }
    }

    public Guid RelationshipId { get; set; }
    public Guid SetId { get; set; }
    public Guid MusicPartId { get; set; }
    public string Name { get; set; }
    public string? Aliases { get; set; }
    public string? PdfDownloadUrl { get; set; }
    public string? DeletePartUrl { get; set; }
}
