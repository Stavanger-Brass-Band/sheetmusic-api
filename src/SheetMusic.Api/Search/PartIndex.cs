using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.ComponentModel.DataAnnotations;

namespace SheetMusic.Api.Search;

[SerializePropertyNamesAsCamelCase]
public class PartIndex
{
    [Key]
    public string Id { get; set; } = null!;

    [IsFilterable, IsSearchable]
    public string PartName { get; set; } = null!;

    [IsFilterable, IsSearchable]
    public string[] Aliases { get; set; } = null!;
}
