using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.OData.MVC
{
    [ModelBinder(typeof(ODataParamResolver))]
    public class ODataQueryParams
    {
        public int? Top { get; set; }
        public int? Skip { get; set; }
        public List<ODataOrderByOption> OrderBy { get; set; } = new List<ODataOrderByOption>();
        public ODataExpression? Filter { get; set; }
        public string? Search { get; set; }
        public List<string> Expand { get; set; } = new List<string>();

        public bool HasFilter => Filter != null;
        public bool HasSearch => !string.IsNullOrEmpty(Search);
        public bool IsEmpty => Top == null && Skip == null && !OrderBy.Any() && Filter == null && string.IsNullOrWhiteSpace(Search);
    }

}
