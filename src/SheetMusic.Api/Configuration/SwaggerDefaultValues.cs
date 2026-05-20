using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace SheetMusic.Api.Configuration;

/// <summary>
/// Represents the Swagger/Swashbuckle operation filter used to document the implicit API version parameter.
/// </summary>
/// <remarks>This <see cref="IOperationFilter"/> is only required due to bugs in the <see cref="SwaggerGenerator"/>.
/// Once they are fixed and published, this class can be removed.</remarks>
public class SwaggerDefaultValues : IOperationFilter
{
    /// <summary>
    /// Applies the filter to the specified operation using the given context.
    /// </summary>
    /// <param name="operation">The operation to apply the filter to.</param>
    /// <param name="context">The current operation filter context.</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            if (parameter.Description == null)
                parameter.Description = description.ModelMetadata?.Description;

            if (parameter.Schema is OpenApiSchema schema && schema.Default == null && description.DefaultValue != null)
            {
                schema.Default = JsonValue.Create(description.DefaultValue.ToString());

                if (parameter.Name.Contains("api-version")) //lock-down version parm
                {
                    schema.Enum = new List<JsonNode?> { JsonValue.Create(description.DefaultValue.ToString()) };
                    parameter.Required = true;
                }
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
