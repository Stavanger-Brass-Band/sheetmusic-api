using SheetMusic.Api.OData.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SheetMusic.Api.OData;

public partial class ODataParser
{
    static readonly List<string> operations = ["eq", "neq", "ne", "gt", "lt", "ge", "le", "=", "==", "!=", ">", ">=", "<", "<=", "in"];
    static readonly List<string> logicalOperators = ["and", "or", "&&", "||"];

    static readonly Regex logicalGroupingPattern = new($@"(\([^()]+(?:{string.Join("|", operations)})[^()]+\)|\([^()]+(?:{string.Join("|", logicalOperators)})[^()]+\))",
        RegexOptions.IgnoreCase);

    private readonly List<string[]> collectionValues = [];
    private readonly List<string> collections = [];
    private readonly List<string> logicalExpressions = [];
    private readonly List<string> values = [];

    private readonly string originalFilter;

    private ODataParser(string filter)
    {
        originalFilter = filter;
    }

    private ODataExpression Parse()
    {
        var processedFilter = TrimUselessGrouping(originalFilter);
        processedFilter = TrimSingleExpressionGrouping(processedFilter);

        // look for collections
        processedFilter = CollectionPattern().Replace(processedFilter, match =>
        {
            collections.Add(match.Value);
            collectionValues.Add(match.Groups[1].Value.Split(',').Select(s => s.Trim('\'')).ToArray());
            return $"__COLLECTION|{collections.Count - 1}";
        });
        processedFilter = QuotedValuePattern().Replace(processedFilter, match =>
        {
            var value = match.Groups[0].Value;
            values.Add(value[1..^1]);
            return $"__VALUE|{values.Count - 1}";
        });


        // look for logical groups
        do
        {
            processedFilter = logicalGroupingPattern.Replace(processedFilter, match =>
            {
                logicalExpressions.Add(match.Value);
                return $"__EXP|{logicalExpressions.Count - 1}";
            });
        }
        while (logicalGroupingPattern.IsMatch(processedFilter));

        var expression = ProcessFilterA(processedFilter);
        return expression;
    }

    public static ODataExpression Parse(string filter)
    {
        var parser = new ODataParser(filter);
        return parser.Parse();
    }

    private ODataExpression ProcessFilterA(string filter)
    {
        var trimmedFilter = TrimUselessGrouping(filter);
        var tokens = LogicalSplitPattern().Split(trimmedFilter)
            .Select(t => t.Trim())
            .ToArray();

        var operatorMatches = LogicalOperatorMatchPattern().Matches(trimmedFilter);
        var operators = operatorMatches.Cast<Match>().Select(m => m.Groups[1].Value).ToArray();


        if (operators.Length == 0)
            return ProcessExpressionToken(tokens[0]);

        // Build tree right to left.
        var reversedTokenList = tokens.Reverse().ToList();
        var operatorStack = new Stack<LogicalOperator>(operators.Reverse().Select(o => ResolveLogicalOperator(o)));

        var seedExpression = new ODataFilterGroup
        {
            Left = ProcessExpressionToken(reversedTokenList[0]),
            Right = ProcessExpressionToken(reversedTokenList[1]),
            Operator = operatorStack.Pop()
        };

        var finalExpression = reversedTokenList.Skip(2)
            .Aggregate<string, ODataExpression>(seedExpression, (right, nextExpToken) => new ODataFilterGroup
            {
                Left = ProcessExpressionToken(nextExpToken),
                Right = right,
                Operator = operatorStack.Pop()
            });

        return finalExpression;
    }


    private ODataFilterExpression CreateExpression(string filter)
    {
        if (string.IsNullOrEmpty(filter))
            throw new InvalidOperationException("Filter cannot be empty");

        var splitPattern = string.Join("|", operations);
        var tokens = Regex.Split(filter, $@"\s({splitPattern})\s", RegexOptions.IgnoreCase)
            .Select(t => t.Trim())
            .ToList();

        var operation = ResolveOperation(tokens[1]);

        var expression = new ODataFilterExpression()
        {
            Field = tokens[0],
            Value = tokens[2],
            Operation = operation
        };

        var value = tokens[2];

        if (value.StartsWith("__VALUE"))
            expression.Value = values[int.Parse(value.Split('|')[1])];

        if (value.StartsWith("__COLLECTION"))
        {
            var collectionIndex = int.Parse(value.Split('|')[1]);
            var collection = collections[collectionIndex];
            value = collection;

            var collectionItemMatch = CollectionItemPattern().Matches(collection);
            var items = collectionItemMatch.Cast<Match>().Select(g => g.Groups[1].Value).ToList();

            expression.IsCollection = true;
            expression.CollectionItems = items.ToArray();
        }

        return expression;
    }

    private ODataExpression ProcessExpressionToken(string token)
    {
        if (token.StartsWith("__EXP"))
            return ExpandFilter(token);
        else
            return CreateExpression(token);
    }

    private ODataExpression ExpandFilter(string expressionCode)
    {
        var index = int.Parse(expressionCode.Split('|')[1]);
        var expression = logicalExpressions[index];
        var grp = ProcessFilterA(expression);

        return grp;
    }

    private static FilterOperation ResolveOperation(string operation) => operation.ToLower() switch
    {
        "eq" or "=" or "==" => FilterOperation.Eq,
        "lt" or "<" => FilterOperation.Lt,
        "le" or "lteq" or "<=" => FilterOperation.Lteq,
        "gt" or ">" => FilterOperation.Gt,
        "ge" or "gteq" or ">=" => FilterOperation.Gteq,
        "ne" or "neq" or "not" or "!=" => FilterOperation.Not,
        "in" => FilterOperation.In,
        _ => throw new ArgumentException("Invalid filter operation: " + operation),
    };

    private static LogicalOperator ResolveLogicalOperator(string logicalOperator) => logicalOperator.ToLower() switch
    {
        "and" or "&&" => LogicalOperator.And,
        "or" or "||" => LogicalOperator.Or,
        _ => throw new InvalidOperationException("Invalid logical operator: " + logicalOperator),
    };

    private static string TrimSingleExpressionGrouping(string filter)
    {
        return SingleExpressionGroupingPattern().Replace(filter, match =>
        {
            // Remove '' as this could contain and/or, and can't find any clean way of checking for this in a regex.
            var cleaned = QuotedTextPattern().Replace(match.Value, "removed");

            if (AndOrWhitespacePattern().IsMatch(cleaned))
                return match.Value;

            return match.Value[1..^1];
        });
    }
    private static string TrimUselessGrouping(string filter)
    {
        if (WrappingParenthesesPattern().IsMatch(filter))
        {
            if (!ParenthesisCharPattern().IsMatch(filter.AsSpan(1, filter.Length - 2)))
                // No other parenthesis in the body, we can trim the start and end
                return filter[1..^1];

            var depth = 1;
            foreach (var character in filter[1..])
            {
                switch (character)
                {
                    case '(': depth++; break;
                    case ')': depth--; break;
                }

                if (depth == 0)
                    // If we reach 0 depth before the end, it means there is a left and right not wrapped in same ( )
                    return filter;
            }

            // Trim start and end - we did not reach equality, meaning the starting ( spans the whole expression.
            return filter[1..^1];
        }

        return filter;
    }

    [GeneratedRegex(@"(\((?:[,\s]*'[^']+')+\))")]
    private static partial Regex CollectionPattern();

    [GeneratedRegex(@"([""'])(?:(?=(\\?))\2.)*?\1")]
    private static partial Regex QuotedValuePattern();

    [GeneratedRegex(@"\band\b|\bor\b|\b&&\b|\b\|\|\b", RegexOptions.IgnoreCase)]
    private static partial Regex LogicalSplitPattern();

    [GeneratedRegex(@"\s(and|or|&&|\|\|)\s", RegexOptions.IgnoreCase)]
    private static partial Regex LogicalOperatorMatchPattern();

    [GeneratedRegex("'([^']+)'")]
    private static partial Regex CollectionItemPattern();

    [GeneratedRegex(@"\([^()]+(?:eq|neq|ne|=|!=|gt|>|lt|<|ge|>=|le|<=)[^()]+\)")]
    private static partial Regex SingleExpressionGroupingPattern();

    [GeneratedRegex(@"'[^']+'")]
    private static partial Regex QuotedTextPattern();

    [GeneratedRegex(@"\s(and|or)\s")]
    private static partial Regex AndOrWhitespacePattern();

    [GeneratedRegex(@"^\(.*\)$")]
    private static partial Regex WrappingParenthesesPattern();

    [GeneratedRegex("[()]")]
    private static partial Regex ParenthesisCharPattern();
}
