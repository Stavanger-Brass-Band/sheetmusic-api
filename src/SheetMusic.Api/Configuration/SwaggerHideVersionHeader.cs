using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

namespace SheetMusic.Api.Configuration;

public class SwaggerHideVersionHeader : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var headerParameter = operation.Parameters.FirstOrDefault(x => x.Name == "x-api-version");
        operation.Parameters.Remove(headerParameter);
    }
}
