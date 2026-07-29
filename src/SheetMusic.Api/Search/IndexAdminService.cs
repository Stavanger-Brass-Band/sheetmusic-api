using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Errors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Search;

public class IndexAdminService(IConfiguration config) : IIndexAdminService
{
    public async Task EnsureIndexAsync<T>()
    {
        var indexClient = new SearchIndexClient(Endpoint, new AzureKeyCredential(AdminKey));
        var fieldBuilder = new FieldBuilder();
        var searchFields = fieldBuilder.Build(typeof(T));

        var definition = new SearchIndex(GetIndexName<T>(), searchFields);
        await indexClient.CreateOrUpdateIndexAsync(definition);
    }

    public async Task FillIndexAsync<T>(IEnumerable<T> items)
    {
        var client = GetQueryClient<T>();
        await client.UploadDocumentsAsync(items);
    }

    public async Task ClearIndexAsync<T>()
    {
        var indexClient = new SearchIndexClient(Endpoint, new AzureKeyCredential(AdminKey));
        await indexClient.DeleteIndexAsync(GetIndexName<T>(), default(MatchConditions));
    }

    public SearchClient GetQueryClient<T>()
    {
        return new SearchClient(Endpoint, GetIndexName<T>(), new AzureKeyCredential(AdminKey));
    }

    private Uri Endpoint => new Uri($"https://{config[ConfigKeys.SearchHost] ?? throw new MissingConfigurationException(ConfigKeys.SearchHost)}");
    private string AdminKey => config[ConfigKeys.SearchAdminKey] ?? throw new MissingConfigurationException(ConfigKeys.SearchAdminKey);

    /// <summary>
    /// Resolves the physical index name for <typeparamref name="T"/>, optionally prefixed by
    /// <see cref="ConfigKeys.SearchIndexPrefix"/> so test and prod can share a single Free-tier
    /// search service without a rebuild in one environment deleting the other's index. With no
    /// prefix configured this returns the historical unprefixed name unchanged.
    /// </summary>
    private string GetIndexName<T>()
    {
        var baseName = typeof(T).Name.ToLower();
        var prefix = config[ConfigKeys.SearchIndexPrefix];
        return string.IsNullOrWhiteSpace(prefix) ? baseName : $"{prefix}-{baseName}";
    }
}
