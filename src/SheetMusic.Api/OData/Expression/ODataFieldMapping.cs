using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SheetMusic.Api.OData.Expression
{
    public class ODataFieldMapping<T>
    {
        public Dictionary<string, LambdaExpression> Mapping { get; } = new Dictionary<string, LambdaExpression>();

        public ODataFieldMapping<T> MapField<TItem>(string queryFieldName, Expression<Func<T, TItem>> expression)
        {
            Mapping.Add(queryFieldName, expression);
            return this;
        }

        internal LambdaExpression GetMapping(string field)
        {
            var item = Mapping.FirstOrDefault(i => string.Equals(i.Key, field, StringComparison.OrdinalIgnoreCase));
            var expression = item.Value;
            if (expression is null)
                throw new ArgumentOutOfRangeException("field", "Could not locate mapping for field " + field);

            return expression;
        }
    }

}
