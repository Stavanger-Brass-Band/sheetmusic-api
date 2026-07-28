using Microsoft.AspNetCore.Mvc.ModelBinding;
using SheetMusic.Api.Errors;
using SheetMusic.Api.OData.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.OData.MVC;

public class ODataParamResolver : IModelBinder
{
    public ODataParamResolver()
    {
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var param = new ODataQueryParams
        {
            Skip = GetIntParam(bindingContext, "$skip"),
            Top = GetIntParam(bindingContext, "$top"),
            Search = GetStringParam(bindingContext, "$search")
        };

        if (param.Top < 1)
            throw new InvalidQueryParametersError("$top must be at least 1 row");

        if (param.Skip < 0)
            throw new InvalidQueryParametersError("$skip cannot be negative");

        var filter = GetStringParam(bindingContext, "$filter");
        var order = GetStringParam(bindingContext, "$orderby");
        var expand = GetStringParam(bindingContext, "$expand");

        if (expand != null)
            param.Expand = ParseExpand(expand);

        param.OrderBy = order != null ? ParseOrderBy(order) : new List<ODataOrderByOption>();

        if (filter != null)
            param.Filter = ParseFilter(filter);

        bindingContext.Result = ModelBindingResult.Success(param);

        return Task.CompletedTask;
    }

    private static List<string> ParseExpand(string expand)
    {
        if (string.IsNullOrWhiteSpace(expand))
            throw new InvalidQueryParametersError("$expand cannot be empty");

        var options = expand.Split(',').Select(o => o.Trim()).ToList();

        if (options.Any(string.IsNullOrEmpty))
            throw new InvalidQueryParametersError($"Invalid $expand clause '{expand}'");

        return options;
    }

    private static List<ODataOrderByOption> ParseOrderBy(string order)
    {
        if (string.IsNullOrWhiteSpace(order))
            throw new InvalidQueryParametersError("$orderby cannot be empty");

        return [.. order.Split(',').Select(clause => ParseOrderByClause(clause, order))];
    }

    private static ODataOrderByOption ParseOrderByClause(string clause, string fullClause)
    {
        var tokens = clause.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length is 0 or > 2)
            throw new InvalidQueryParametersError(InvalidOrderByMessage(fullClause));

        if (!IsFieldName(tokens[0]))
            throw new InvalidQueryParametersError(InvalidOrderByMessage(fullClause));

        var direction = SortDirection.asc;

        if (tokens.Length == 2)
        {
            if (string.Equals(tokens[1], "asc", StringComparison.OrdinalIgnoreCase))
                direction = SortDirection.asc;
            else if (string.Equals(tokens[1], "desc", StringComparison.OrdinalIgnoreCase))
                direction = SortDirection.desc;
            else
                throw new InvalidQueryParametersError($"Invalid sort direction '{tokens[1]}' in $orderby clause '{fullClause}'. Must be 'asc' or 'desc'");
        }

        return new ODataOrderByOption
        {
            Field = tokens[0],
            Direction = direction
        };
    }

    /// <summary>
    /// <c>$orderby</c> uses OData syntax, not JSON. Without this check a serialized object such as
    /// <c>[{"field":"title","direction":0}]</c> is split on its commas into fragments that are accepted as
    /// field names here, and only fail much later with a confusing "could not locate mapping" message.
    /// </summary>
    private static bool IsFieldName(string token) =>
        char.IsLetter(token[0]) && token.All(c => char.IsLetterOrDigit(c) || c == '_');

    private static string InvalidOrderByMessage(string fullClause) =>
        $"Invalid $orderby clause '{fullClause}'. Expected comma separated clauses on the format 'field [asc|desc]', for example 'title desc'";

    private static ODataExpression ParseFilter(string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            throw new InvalidQueryParametersError("$filter cannot be empty");

        try
        {
            return ODataParser.Parse(filter);
        }
        catch (Exception ex)
        {
            throw new InvalidQueryParametersError($"Invalid $filter clause '{filter}'", ex);
        }
    }

    private static string? GetStringParam(ModelBindingContext bindingContext, string fieldName)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(fieldName);
        if (valueProviderResult != ValueProviderResult.None)
        {
            var value = valueProviderResult.FirstValue;
            return value;
        }
        return null;
    }

    private static int? GetIntParam(ModelBindingContext bindingContext, string fieldName)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(fieldName);
        if (valueProviderResult == ValueProviderResult.None)
            return null;

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidQueryParametersError($"{fieldName} cannot be empty");

        if (!int.TryParse(value, out var parsed))
            throw new InvalidQueryParametersError($"{fieldName} must be a whole number, but was '{value}'");

        return parsed;
    }
}

