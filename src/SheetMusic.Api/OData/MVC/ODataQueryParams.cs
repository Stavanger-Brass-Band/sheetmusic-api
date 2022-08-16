using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SheetMusic.Api.OData.MVC;

[ModelBinder(typeof(ODataParamResolver))]
public class ODataQueryParams
{
    [JsonPropertyName("$top")]
    public int? Top { get; set; }

    [JsonPropertyName("$skip")]
    public int? Skip { get; set; }

    [JsonPropertyName("$orderBy")]
    public List<ODataOrderByOption> OrderBy { get; set; } = new List<ODataOrderByOption>();

    [JsonPropertyName("$filter")]
    public ODataExpression? Filter { get; set; }
    
    [JsonPropertyName("$search")]
    public string? Search { get; set; }

    [JsonPropertyName("$expand")]
    public List<string> Expand { get; set; } = new List<string>();

    public bool HasFilter => Filter != null;
    public bool HasSearch => !string.IsNullOrEmpty(Search);
    public bool IsEmpty => Top == null && Skip == null && !OrderBy.Any() && Filter == null && string.IsNullOrWhiteSpace(Search);
}

