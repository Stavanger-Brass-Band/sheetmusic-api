using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SheetMusic.Api.Search;

public class PartIndex
{
    [Key]
    [SimpleField(IsKey = true)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [SearchableField(IsFilterable = true)]
    [JsonPropertyName("partName")]
    public string PartName { get; set; } = null!;

    [SearchableField(IsFilterable = true)]
    [JsonPropertyName("aliases")]
    public string[] Aliases { get; set; } = null!;
}
