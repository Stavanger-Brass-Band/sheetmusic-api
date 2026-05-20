using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace SheetMusic.Api.Configuration;

public class SwaggerHideVersionHeader : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            return;

        var headerParameter = operation.Parameters.OfType<OpenApiParameter>().FirstOrDefault(x => x.Name == "x-api-version");
        if (headerParameter != null)
            operation.Parameters.Remove(headerParameter);
    }
}
