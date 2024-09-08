using SheetMusic.Api.OData.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SheetMusic.Api.OData.Expression;

public class ODataFilterExpressionBuilder<T>
{
    private readonly ODataExpression oDataExpression;
    private readonly ParameterExpression _parameter;
    private readonly ODataFieldMapping<T> fieldMapping;

    // A bug in the cosmos db linq provider requires the parameter to be "root" in complex expressions
    private const string PARAMETER_NAME = "root";

    public ODataFilterExpressionBuilder(ODataExpression? expression, Action<ODataFieldMapping<T>> mapFields)
    {
        if (expression is null)
            throw new ArgumentNullException(nameof(expression));

        oDataExpression = expression;
        _parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), PARAMETER_NAME);

        fieldMapping = new ODataFieldMapping<T>();
        mapFields(fieldMapping);

        EnsureProperParameterName(fieldMapping);
    }


    public Expression<Func<T, bool>> FilterLambda => BuildLambda();

    public IEnumerable<T> FilterCollection(IEnumerable<T> collection)
    {
        return collection.Where(FilterLambda.Compile());
    }

    #region Lambda builders
    private Expression<Func<T, bool>> BuildLambda()
    {
        var expression = BuildExpression(oDataExpression);

        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expression, _parameter);
    }
    #endregion

    /// <summary>
    /// Update the expressions in the provided field mapping to use the correct root param name.
    /// </summary>
    /// <param name="fieldMappings"></param>
    private void EnsureProperParameterName(ODataFieldMapping<T> fieldMappings)
    {
        var renamedMappings = new Dictionary<string, Expression<Func<T, object>>>();

        foreach (var key in fieldMappings.Mapping.Keys.ToList())
        {
            var item = fieldMapping.Mapping[key];
            var renamed = (LambdaExpression)new RenameParameterVisitor(_parameter).Visit(item);
            fieldMapping.Mapping[key] = renamed;
        }
    }

    #region Expression builders

    private System.Linq.Expressions.Expression BuildExpression()
    {
        var expression = BuildExpression(oDataExpression);
        return expression;
    }

    private System.Linq.Expressions.Expression BuildExpression(ODataExpression expression)
    {
        switch (expression.Type)
        {
            case Constants.ExpressionType.FilterValue:
                var filterExp = BuildExpression((ODataFilterExpression)expression);
                return filterExp;

            case Constants.ExpressionType.Group:
                var groupFilterExp = BuildExpression((ODataFilterGroup)expression);
                return groupFilterExp;

            default:
                throw new ArgumentException($"Unsupported odata expression type found '{expression.Type}'");
        }
    }

    private System.Linq.Expressions.Expression BuildExpression(ODataFilterExpression filterExp)
    {
        var mapping = fieldMapping.GetMapping(filterExp.Field);

        var left = mapping.Body;

        // Get param in correct type
        var value = GetObjectValue(left.Type, filterExp);

        if (filterExp.Operation == FilterOperation.In)
        {
            if (left is not MemberExpression memberExp)
                throw new ArgumentException($"Field {filterExp.Field} is used with 'in' operator, but does not generate a member expression. To use 'in' operator the field has to be mapped to a member");

            if (value is null)
                return System.Linq.Expressions.Expression.Empty();

            var containsCondition = CreateContainsPredicate(memberExp, value);
            return containsCondition;
        }

        var right = System.Linq.Expressions.Expression.Constant(value, left.Type);
        var condition = CreateConditionExpression(left, right, filterExp.Operation);
        return condition;
    }

    private BinaryExpression BuildExpression(ODataFilterGroup filterGroup)
    {
        var left = BuildExpression(filterGroup.Left);
        var right = BuildExpression(filterGroup.Right);

        switch (filterGroup.Operator)
        {
            case LogicalOperator.And:
                return System.Linq.Expressions.Expression.AndAlso(left, right);

            case LogicalOperator.Or:
                return System.Linq.Expressions.Expression.OrElse(left, right);

            default:
                throw new ArgumentException($"Unsupported operator used in filtergroup '{filterGroup.Operator}'");
        }
    }

    private System.Linq.Expressions.Expression CreateContainsPredicate(MemberExpression member, object value)
    {
        var method = typeof(Enumerable)
            .GetRuntimeMethods()
            .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

        var arrayType = value.GetType().GetElementType();
        var containsMethod = method.MakeGenericMethod(member.Type);

        var exprContains = System.Linq.Expressions.Expression.Call(containsMethod, new System.Linq.Expressions.Expression[] { System.Linq.Expressions.Expression.Constant(value, member.Type.MakeArrayType()), member });
        return exprContains;
    }


    private object? GetObjectValue(Type type, ODataFilterExpression filterExp)
    {
        #region Handle collections

        if (filterExp.IsCollection)
        {
            var arrayType = type.MakeArrayType();
            var array = Activator.CreateInstance(arrayType, filterExp.CollectionItems.Length);
            var setMethod = arrayType.GetMethod("Set");

            for (var i = 0; i < filterExp.CollectionItems.Length; i++)
            {
                var typedValue = GetObjectValue(type, filterExp.CollectionItems[i]);

                if (typedValue is not null)
                    setMethod?.Invoke(array, new object[] { i, typedValue });
            }
            return array;
        }

        #endregion

        return GetObjectValue(type, filterExp.Value);
    }

    private object? GetObjectValue(Type type, string value)
    {
        #region Handle nullable types

        var isNullable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        if (string.IsNullOrEmpty(value) || value.ToLower() == "null")
            return null;

        // Nullable type is not null, fall back to the native type and parse as usual.
        type = isNullable ? type.GetGenericArguments()[0] : type;

        #endregion

        object filterValue = null!;
        switch (type)
        {
            case var t when t == typeof(Guid):
                if (Guid.TryParse(value, out var guidValue))
                    filterValue = guidValue;
                else
                    throw new ArgumentException("Not a valid guid");

                break;

            case var t when t == typeof(DateTimeOffset):
                if (value?.ToLower() == "today")
                    filterValue = DateTimeOffset.UtcNow.Date;
                else if (DateTimeOffset.TryParse(value, out var dateValue))
                    filterValue = dateValue;
                else
                    throw new ArgumentException("Not a valid date");
                break;

            case var t when t == typeof(DateTime):
                if (value?.ToLower() == "today")
                    filterValue = DateTime.UtcNow.Date;
                else if (DateTime.TryParse(value, out var dateValue))
                    filterValue = dateValue;
                else
                    throw new ArgumentException("Not a valid date");
                break;

            case var t when t == typeof(string):
                filterValue = value;
                break;

            case var t when t == typeof(int):
                if (int.TryParse(value, out var intValue))
                    filterValue = intValue;
                else
                    throw new ArgumentException("Not a valid int");
                break;

            case var t when t == typeof(double):
                if (double.TryParse(value, out var doubleValue))
                    filterValue = doubleValue;
                else
                    throw new ArgumentException("Not a valid double");
                break;

            case var t when t == typeof(float):
                if (float.TryParse(value, out var floatValue))
                    filterValue = floatValue;
                else
                    throw new ArgumentException("Not a valid float");
                break;

            case var t when t == typeof(bool):
                if (bool.TryParse(value, out var boolValue))
                    filterValue = boolValue;
                else
                    throw new ArgumentException("Not a valid bool");
                break;

            case var t when t.IsEnum:
                filterValue = GetEnumValue(t, value);
                break;

            default:
                throw new NotImplementedException("Field type not implemented");
        }

        return filterValue;
    }
    private object GetEnumValue(Type enumType, string enumString)
    {
        try
        {
            var value = Enum.Parse(enumType, enumString, true);
            return value;
        }
        catch (Exception)
        {
            var supportedValues = string.Join(", ", Enum.GetNames(enumType).Select(v => $"'{v}'"));
            throw new ArgumentException($"Invalid value provided ({enumString}), must be {supportedValues}.");
        }
    }

    private static BinaryExpression CombineWithLogicalExpression(BinaryExpression left, BinaryExpression right, LogicalOperator? logicalOperator)
    {
        if (logicalOperator == null || left == null)
            return right;

        if (logicalOperator == LogicalOperator.And)
            return System.Linq.Expressions.Expression.AndAlso(left, right);

        return System.Linq.Expressions.Expression.OrElse(left, right);
    }

    private static BinaryExpression CreateConditionExpression(System.Linq.Expressions.Expression left, System.Linq.Expressions.Expression right, FilterOperation operation)
    {
        switch (operation)
        {
            case FilterOperation.Eq:
                return System.Linq.Expressions.Expression.Equal(left, right);

            case FilterOperation.Gt:
                return System.Linq.Expressions.Expression.GreaterThan(left, right);

            case FilterOperation.Lt:
                return System.Linq.Expressions.Expression.LessThan(left, right);

            case FilterOperation.Gteq:
                return System.Linq.Expressions.Expression.GreaterThanOrEqual(left, right);

            case FilterOperation.Lteq:
                return System.Linq.Expressions.Expression.LessThanOrEqual(left, right);

            case FilterOperation.Not:
                return System.Linq.Expressions.Expression.NotEqual(left, right);
        }

        throw new InvalidOperationException("Invalid filter operation");
    }

    /// <summary>
    /// Renames the expression root to provided expression.
    /// </summary>
    private class RenameParameterVisitor(ParameterExpression parameter) : ExpressionVisitor
    {
        protected override System.Linq.Expressions.Expression VisitParameter(ParameterExpression originalParameter)
        {
            return parameter;
        }
    }
    #endregion

}

