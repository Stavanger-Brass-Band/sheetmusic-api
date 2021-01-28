using SheetMusic.Api.OData.Expression;
using SheetMusic.Api.OData.MVC;
using System;
using System.Linq;

namespace SheetMusic.Api.OData
{
    public static class ODataQueryParamsExtensions
    {
        public static ODataFilterExpression? GetFilterForField(this ODataExpression expression, string fieldName)
        {
            return expression.GetFilters().FirstOrDefault(f => string.Equals(f.Field, fieldName, StringComparison.InvariantCultureIgnoreCase));
        }

        public static ODataFilterExpressionBuilder<T> GenerateFilters<T>(this ODataQueryParams queryParams, Action<ODataFieldMapping<T>> mapFields)
        {
            var builder = new ODataFilterExpressionBuilder<T>(queryParams.Filter, mapFields);
            return builder;
        }

        public static IQueryable<T> ApplyODataFilters<T>(this IQueryable<T> items, ODataQueryParams queryParams, Action<ODataFieldMapping<T>> mapFields)
        {
            var builder = new ODataFilterExpressionBuilder<T>(queryParams.Filter, mapFields);
            return items.Where(builder.FilterLambda);
        }
    }
}
