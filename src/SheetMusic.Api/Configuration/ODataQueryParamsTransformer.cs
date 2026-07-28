using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using SheetMusic.Api.OData.MVC;
using System.Linq;

namespace SheetMusic.Api.Configuration;

/// <summary>
/// <see cref="ODataQueryParams"/> is bound by <see cref="ODataParamResolver"/> from flat, string based query
/// parameters such as <c>$orderby=title desc</c>. The native OpenAPI generator however sees it as a complex
/// type and documents it either as a single object shaped parameter or as one parameter per property, neither
/// of which the binder reads. Clients would consequently send serialized JSON (for example
/// <c>$orderBy=[{"field":"title","direction":0}]</c>), which is rejected as an invalid clause. This transformer
/// replaces those parameters with the ones actually supported.
/// </summary>
public class ODataQueryParamsTransformer : IOpenApiOperationTransformer
{
    private static readonly (string Name, string Description, JsonSchemaType Type)[] ODataParameters =
    [
        ("$search", "Free text search across archive number, title, composer and arranger.", JsonSchemaType.String),
        ("$filter", "OData filter expression, for example \"title eq 'Fanfare'\" or \"archiveNumber gt 100\".", JsonSchemaType.String),
        ("$orderby", "Comma separated sort clauses on the format \"field [asc|desc]\", for example \"composer asc,title desc\".", JsonSchemaType.String),
        ("$top", "Maximum number of rows to return. Must be at least 1.", JsonSchemaType.Integer),
        ("$skip", "Number of rows to skip before returning results.", JsonSchemaType.Integer),
        ("$expand", "Comma separated list of related collections to include, for example \"parts\".", JsonSchemaType.String)
    ];

    /// <inheritdoc />
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.Parameters == null)
            return Task.CompletedTask;

        var generatedNames = context.Description.ParameterDescriptions
            .Where(IsODataQueryParam)
            .Select(p => p.Name)
            .ToHashSet();

        if (generatedNames.Count == 0)
            return Task.CompletedTask;

        foreach (var parameter in operation.Parameters.Where(p => generatedNames.Contains(p.Name!)).ToList())
            operation.Parameters.Remove(parameter);

        foreach (var (name, description, type) in ODataParameters)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = name,
                In = ParameterLocation.Query,
                Required = false,
                Description = description,
                Schema = new OpenApiSchema { Type = type }
            });
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Depending on the binding source, ApiExplorer either reports the whole <see cref="ODataQueryParams"/>
    /// object as one parameter or flattens it into one parameter per (nested) property. Both shapes are wrong,
    /// so both are matched through the parameter descriptor they all originate from.
    /// </summary>
    private static bool IsODataQueryParam(ApiParameterDescription parameter) =>
        parameter.ParameterDescriptor?.ParameterType == typeof(ODataQueryParams)
        || parameter.ModelMetadata?.ModelType == typeof(ODataQueryParams)
        || parameter.ModelMetadata?.ContainerType == typeof(ODataQueryParams);
}
