using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SheetMusic.Api.Configuration;

/// <summary>
/// Sets the <see cref="OpenApiInfo"/> for an OpenAPI document, using the metadata of the API version the
/// document was generated for.
/// </summary>
/// <param name="description">The API version description the document is generated for.</param>
public class ApiVersionDocumentInfoTransformer(ApiVersionDescription description) : IOpenApiDocumentTransformer
{
    /// <inheritdoc />
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var apiDescription = "API for Stavanger Brass Bands sheet music archive";

        if (description.IsDeprecated)
            apiDescription += " This API version has been deprecated.";

        document.Info = new OpenApiInfo
        {
            Title = "Sheetmusic API",
            Version = description.ApiVersion.ToString(),
            Description = apiDescription,
            Contact = new OpenApiContact { Name = "Leif Bjarte Johansson", Email = "leif.bjarte@gmail.com" }
        };

        return Task.CompletedTask;
    }
}
