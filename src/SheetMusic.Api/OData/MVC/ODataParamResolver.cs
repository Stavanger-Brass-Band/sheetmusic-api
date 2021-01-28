using Microsoft.AspNetCore.Mvc.ModelBinding;
using SheetMusic.Api.OData.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.OData.MVC
{
    public class ODataParamResolver : IModelBinder
    {
        public ODataParamResolver()
        {
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var param = new ODataQueryParams();

            param.Skip = GetIntParam(bindingContext, "$skip");
            param.Top = GetIntParam(bindingContext, "$top");

            param.Search = GetStringParam(bindingContext, "$search");

            var filter = GetStringParam(bindingContext, "$filter");
            var order = GetStringParam(bindingContext, "$orderby");

            var expand = GetStringParam(bindingContext, "$expand");

            if (expand != null)
            {
                try
                {
                    param.Expand = new List<string>(expand.Split(','));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Invalid expand clause", ex);
                }
            }

            if (order != null)
            {
                try
                {
                    var orderoptions = order.Split(',')
                        .Select(t => t.Trim().Split(' ')).Select(t => new ODataOrderByOption()
                        {
                            Field = t[0].Trim(),
                            Direction = t.Length == 1 ? SortDirection.asc : t[1].Trim() == "asc" ? SortDirection.asc : SortDirection.desc
                        });
                    param.OrderBy = orderoptions.ToList();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException("Invalid order by clause", ex);
                }
            }
            else
            {
                param.OrderBy = new List<ODataOrderByOption>();
            }

            if (filter != null)
                param.Filter = ODataParser.Parse(filter);

            bindingContext.Result = ModelBindingResult.Success(param);

            return Task.CompletedTask;
        }


        private string? GetStringParam(ModelBindingContext bindingContext, string fieldName)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(fieldName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                var value = valueProviderResult.FirstValue;
                return value;
            }
            return null;
        }

        private int? GetIntParam(ModelBindingContext bindingContext, string fieldName)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(fieldName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                var value = valueProviderResult.FirstValue;
                if (int.TryParse(value, out var fvalue))
                    return fvalue;
            }
            return null;
        }

    }

}
