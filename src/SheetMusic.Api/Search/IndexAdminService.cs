using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Search;

public class IndexAdminService(SearchIndexClient searchIndexClient, IConfiguration config) : IIndexAdminService
{
    public async Task EnsureIndexAsync<T>()
    {
        var fieldBuilder = new FieldBuilder();
        var searchFields = fieldBuilder.Build(typeof(T));

        var definition = new SearchIndex(GetIndexName<T>(), searchFields);
        await searchIndexClient.CreateOrUpdateIndexAsync(definition);
    }

    public async Task FillIndexAsync<T>(IEnumerable<T> items)
    {
        var client = GetQueryClient<T>();
        await client.UploadDocumentsAsync(items);
    }

    public async Task ClearIndexAsync<T>()
    {
        await searchIndexClient.DeleteIndexAsync(GetIndexName<T>(), default(MatchConditions));
    }

    public SearchClient GetQueryClient<T>()
    {
        return searchIndexClient.GetSearchClient(GetIndexName<T>());
    }

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
