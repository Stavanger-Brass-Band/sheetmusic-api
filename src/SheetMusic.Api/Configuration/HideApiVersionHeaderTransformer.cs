using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Linq;

namespace SheetMusic.Api.Configuration;

/// <summary>
/// Removes the internal <c>x-api-version</c> header parameter from the generated OpenAPI document; it is an
/// implementation detail of API version resolution and not a parameter clients should send.
/// </summary>
public class HideApiVersionHeaderTransformer : IOpenApiOperationTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.Parameters == null)
            return Task.CompletedTask;

        var headerParameter = operation.Parameters.OfType<OpenApiParameter>().FirstOrDefault(x => x.Name == "x-api-version");
        if (headerParameter != null)
            operation.Parameters.Remove(headerParameter);

        return Task.CompletedTask;
    }
}
