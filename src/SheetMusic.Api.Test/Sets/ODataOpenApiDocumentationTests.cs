using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Sets;

/// <summary>
/// The OData query parameters are bound from flat strings, but ApiExplorer sees <c>ODataQueryParams</c> as a
/// complex type. Left alone it documents either an object shaped parameter or one parameter per property,
/// making clients send serialized JSON such as <c>$orderBy=[{"field":"title","direction":0}]</c> instead
/// of the OData syntax the binder expects.
/// </summary>
[Collection(Collections.SetList)]
public class ODataOpenApiDocumentationTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    private static readonly string[] ExpectedParameters = ["$search", "$filter", "$orderby", "$top", "$skip", "$expand"];

    private static readonly string[] UnsupportedParameters =
        ["queryParams", "query", "Top", "Skip", "OrderBy", "Filter", "Filter.Type", "Search", "Expand", "HasFilter", "HasSearch", "IsEmpty"];

    [Theory]
    [InlineData("2.0", "/sheetmusic/sets", "category")]
    [InlineData("1.0", "/sheetmusic/sets", "category")]
    [InlineData("1.0", "/categories", null)]
    [InlineData("1.0", "/parts", null)]
    [InlineData("1.0", "/projects", null)]
    public async Task OpenApiDocument_ShouldExposeFlatODataQueryParameters(string apiVersion, string path, string? unrelatedParameter)
    {
        using var document = await GetOpenApiDocumentAsync(apiVersion);

        var parameters = document.RootElement
            .GetProperty("paths")
            .GetProperty(path)
            .GetProperty("get")
            .GetProperty("parameters")
            .EnumerateArray()
            .ToList();

        var names = parameters.Select(p => p.GetProperty("name").GetString()).ToList();

        names.Should().Contain(ExpectedParameters);
        names.Should().NotContain(UnsupportedParameters);
        names.Should().Contain("api-version", "parameters unrelated to OData must survive the filter");

        if (unrelatedParameter is not null)
            names.Should().Contain(unrelatedParameter, "parameters unrelated to OData must survive the filter");

        foreach (var parameter in parameters.Where(p => ExpectedParameters.Contains(p.GetProperty("name").GetString())))
        {
            parameter.GetProperty("in").GetString().Should().Be("query");
            parameter.GetProperty("schema").TryGetProperty("$ref", out _).Should().BeFalse("the binder reads flat values, not objects");
        }
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    public async Task OpenApiDocument_ShouldNeverDocumentODataQueryParamsAsAnObject(string apiVersion)
    {
        using var document = await GetOpenApiDocumentAsync(apiVersion);

        var offenders = new List<string>();

        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                if (!operation.Value.TryGetProperty("parameters", out var parameters))
                    continue;

                var referencesODataQueryParams = parameters.EnumerateArray().Any(p =>
                    p.TryGetProperty("schema", out var schema)
                    && schema.TryGetProperty("$ref", out var reference)
                    && reference.GetString()?.EndsWith("ODataQueryParams") == true);

                if (referencesODataQueryParams)
                    offenders.Add($"{operation.Name.ToUpperInvariant()} {path.Name}");
            }
        }

        offenders.Should().BeEmpty();
    }

    private async Task<JsonDocument> GetOpenApiDocumentAsync(string apiVersion)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        return JsonDocument.Parse(await client.GetStringAsync($"openapi/{apiVersion}.json"));
    }
}
