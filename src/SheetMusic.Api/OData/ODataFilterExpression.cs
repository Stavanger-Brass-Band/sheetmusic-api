using SheetMusic.Api.OData.Constants;
using System.Collections.Generic;

namespace SheetMusic.Api.OData;

public class ODataFilterExpression : ODataExpression
{
    public string Field { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool IsCollection { get; set; }
    public string[] CollectionItems { get; set; } = null!;
    public FilterOperation Operation { get; set; }
    public override ExpressionType Type => ExpressionType.FilterValue;

    public override IEnumerable<ODataFilterExpression> GetFilters() => new[] { this };

}
