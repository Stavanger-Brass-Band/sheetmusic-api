using SheetMusic.Api.OData.Constants;
using System.Collections.Generic;

namespace SheetMusic.Api.OData;

public abstract class ODataExpression
{
    public abstract ExpressionType Type { get; }
    public abstract IEnumerable<ODataFilterExpression> GetFilters();
}

