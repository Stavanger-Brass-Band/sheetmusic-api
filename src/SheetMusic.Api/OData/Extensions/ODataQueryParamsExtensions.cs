using SheetMusic.Api.OData.Constants;
using SheetMusic.Api.OData.Expression;
using SheetMusic.Api.OData.MVC;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SheetMusic.Api.OData;

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

    /// <summary>
    /// Applies the ordering requested via <c>$orderby</c> to the query, using the same field mapping
    /// convention as <see cref="ApplyODataFilters{T}"/>. Multiple order-by fields are applied in the
    /// order they were supplied.
    /// </summary>
    public static IQueryable<T> ApplyODataOrderBy<T>(this IQueryable<T> items, ODataQueryParams queryParams, Action<ODataFieldMapping<T>> mapFields)
    {
        if (!queryParams.OrderBy.Any())
            return items;

        var fieldMapping = new ODataFieldMapping<T>();
        mapFields(fieldMapping);

        IOrderedQueryable<T>? ordered = null;
        foreach (var option in queryParams.OrderBy)
        {
            var keySelector = fieldMapping.GetMapping(option.Field);
            ordered = ordered is null
                ? InvokeOrderMethod(items, keySelector, option.Direction == SortDirection.desc ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy))
                : InvokeOrderMethod(ordered, keySelector, option.Direction == SortDirection.desc ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy));
        }

        return ordered!;
    }

    private static IOrderedQueryable<T> InvokeOrderMethod<T>(IQueryable<T> source, LambdaExpression keySelector, string methodName)
    {
        var method = typeof(Queryable).GetMethods()
            .Single(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), keySelector.ReturnType);

        return (IOrderedQueryable<T>)method.Invoke(null, new object[] { source, keySelector })!;
    }
}
