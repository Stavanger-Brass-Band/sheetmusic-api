using SheetMusic.Api.OData.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SheetMusic.Api.OData
{
    public class ODataParser
    {
        static List<string> operations = new List<string>() { "eq", "neq", "gt", "lt", "ge", "le", "=", "==", "!=", ">", ">=", "<", "<=", "in" };
        static List<string> logicalOperators = new List<string>() { "and", "or", "&&", "||" };

        static Regex logicalGroupingPattern = new Regex($@"(\([^()]+(?:{string.Join("|", operations)})[^()]+\)|\([^()]+(?:{string.Join("|", logicalOperators)})[^()]+\))",
            RegexOptions.IgnoreCase);

        private List<string[]> collectionValues = new List<string[]>();
        private List<string> collections = new List<string>();
        private List<string> logicalExpressions = new List<string>();
        private List<string> values = new List<string>();

        private readonly string originalFilter;

        private ODataParser(string filter)
        {
            originalFilter = filter;

            ValidateFilter();
        }

        private void ValidateFilter()
        {
        }

        private ODataExpression Parse()
        {
            var processedFilter = TrimUselessGrouping(originalFilter);
            processedFilter = TrimSingleExpressionGrouping(processedFilter);

            // look for collections
            processedFilter = Regex.Replace(processedFilter, @"(\((?:[,\s]*'[^']+')+\))", match =>
            {
                collections.Add(match.Value);
                collectionValues.Add(match.Groups[1].Value.Split(',').Select(s => s.Trim('\'')).ToArray());
                return $"__COLLECTION|{collections.Count - 1}";
            });
            processedFilter = Regex.Replace(processedFilter, /*@"'([^']+)'"*/ @"([""'])(?:(?=(\\?))\2.)*?\1", match =>
            {
                var value = match.Groups[0].Value;
                values.Add(value.Substring(1, value.Length - 2));
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
            var tokens = Regex.Split(trimmedFilter, @"\band\b|\bor\b|\b&&\b|\b\|\|\b", RegexOptions.IgnoreCase)
                .Select(t => t.Trim())
                .ToArray();

            var operatorMatches = Regex.Matches(trimmedFilter, $@"\s(and|or|&&|\|\|)\s", RegexOptions.IgnoreCase);
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


        private ODataExpression CreateExpression(string filter)
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

                var collectionItemMatch = Regex.Matches(collection, "'([^']+)'");
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

        private static FilterOperation ResolveOperation(string operation)
        {
            switch (operation.ToLower())
            {
                case "eq": case "=": case "==": return FilterOperation.Eq;
                case "lt": case "<": return FilterOperation.Lt;
                case "lteq": case "<=": return FilterOperation.Lteq;
                case "gt": case ">": return FilterOperation.Gt;
                case "gteq": case ">=": return FilterOperation.Gteq;
                case "not": case "!=": return FilterOperation.Not;
                case "in": return FilterOperation.In;
                default:
                    throw new ArgumentException("Invalid filter operation: " + operation);
            }
        }

        private LogicalOperator ResolveLogicalOperator(string logicalOperator)
        {
            switch (logicalOperator.ToLower())
            {
                case "and": case "&&": return LogicalOperator.And;
                case "or": case "||": return LogicalOperator.Or;
                default:
                    throw new InvalidOperationException("Invalid logical operator: " + logicalOperator);
            }
        }


        private static string TrimSingleExpressionGrouping(string filter)
        {
            var operatorList = string.Join("|", operations);
            var expression = @"\([^()]+(?:eq|neq|=|!=|gt|>|lt|<|ge|>=|le|<=)[^()]+\)";
            return Regex.Replace(filter, expression, match =>
            {
                // Remove '' as this could contain and/or, and can't find any clean way of checking for this in a regex.
                var cleaned = Regex.Replace(match.Value, @"'[^']+'", "removed");

                if (Regex.IsMatch(cleaned, @"\s(and|or)\s"))
                    return match.Value;

                return match.Value.Substring(1, match.Value.Length - 2);
            });
        }
        private static string TrimUselessGrouping(string filter)
        {
            if (Regex.IsMatch(filter, @"^\(.*\)$")) //filter.StartsWith("(") && filter.EndsWith(")"))
            {
                if (!Regex.IsMatch(filter.Substring(1, filter.Length - 2), "[()]"))
                    // No other parenthesis in the body, we can trim the start and end
                    return filter.Substring(1, filter.Length - 2);

                var depth = 1;
                foreach (var character in filter.Substring(1))
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
                return filter.Substring(1, filter.Length - 2);
            }

            return filter;
        }
    }
}
