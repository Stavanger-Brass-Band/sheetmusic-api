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
        await indexClient.DeleteIndexAsync(GetIndexName<T>());
    }

    public SearchClient GetQueryClient<T>()
    {
        return new SearchClient(Endpoint, GetIndexName<T>(), new AzureKeyCredential(AdminKey));
    }

    private Uri Endpoint => new Uri($"https://{config[ConfigKeys.SearchHost] ?? throw new MissingConfigurationException(ConfigKeys.SearchHost)}");
    private string AdminKey => config[ConfigKeys.SearchAdminKey] ?? throw new MissingConfigurationException(ConfigKeys.SearchAdminKey);

    private string GetIndexName<T>() => typeof(T).Name.ToLower();
}
