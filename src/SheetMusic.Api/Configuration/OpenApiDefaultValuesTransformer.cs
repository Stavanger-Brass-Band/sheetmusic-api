using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Linq;
using System.Text.Json.Nodes;

namespace SheetMusic.Api.Configuration;

/// <summary>
/// Fills in parameter descriptions and default values from <see cref="Microsoft.AspNetCore.Mvc.ApiExplorer.ApiParameterDescription"/>
/// that the native OpenAPI generator leaves blank, and locks the implicit <c>api-version</c> parameter down to
/// the single value supported by each operation.
/// </summary>
public class OpenApiDefaultValuesTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var apiDescription = context.Description;

        if (operation.Parameters == null)
            return Task.CompletedTask;

        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

            if (parameter.Description == null)
                parameter.Description = description.ModelMetadata?.Description;

            if (parameter.Schema is OpenApiSchema schema && schema.Default == null && description.DefaultValue != null)
            {
                var defaultValue = description.DefaultValue.ToString() ?? string.Empty;
                schema.Default = (JsonNode?)JsonValue.Create(defaultValue);

                if (parameter.Name?.Contains("api-version") == true) //lock-down version parm
                {
                    schema.Enum = [(JsonNode)JsonValue.Create(defaultValue)!];
                    parameter.Required = true;
                }
            }

            parameter.Required |= description.IsRequired;
        }

        return Task.CompletedTask;
    }
}
