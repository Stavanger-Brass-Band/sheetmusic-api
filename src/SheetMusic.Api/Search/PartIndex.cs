using Azure.Search.Documents.Indexes;
using System.Text.Json.Serialization;

namespace SheetMusic.Api.Search;

public class PartIndex
{
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
