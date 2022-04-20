using SheetMusic.Api.OData.Constants;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.OData;

public class ODataFilterGroup : ODataExpression
{
    public ODataExpression Left { get; set; } = null!;

    public ODataExpression Right { get; set; } = null!;

    public LogicalOperator Operator { get; set; }

    public override ExpressionType Type => ExpressionType.Group;

    public override IEnumerable<ODataFilterExpression> GetFilters() => Left.GetFilters().Union(Right.GetFilters());
}
