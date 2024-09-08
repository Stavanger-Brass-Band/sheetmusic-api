using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Errors;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SheetMusic.Api.Search;

public class IndexAdminService(IConfiguration config) : IIndexAdminService
{
    public async Task EnsureIndexAsync<T>()
    {
        var model = new Index
        {
            Name = GetIndexName<T>(),
            Fields = FieldBuilder.BuildForType<T>()
        };

        var serviceClient = new SearchServiceClient(Host, new SearchCredentials(AdminKey));
        await serviceClient.Indexes.CreateOrUpdateAsync(model);
    }

    public async Task FillIndexAsync<T>(IEnumerable<T> items)
    {
        var indexClient = new SearchIndexClient(Host, GetIndexName<T>(), new SearchCredentials(AdminKey));
        var batch = IndexBatch.New(items.Select(IndexAction.Upload));

        await indexClient.Documents.IndexAsync(batch);
    }

    public async Task ClearIndexAsync<T>()
    {
        var serviceClient = new SearchServiceClient(Host, new SearchCredentials(AdminKey));
        await serviceClient.Indexes.DeleteAsync(GetIndexName<T>());
    }

    public SearchIndexClient GetQueryClient<T>()
    {
        return new SearchIndexClient(Host, GetIndexName<T>(), new SearchCredentials(AdminKey));
    }

    private string Host => config[ConfigKeys.SearchHost] ?? throw new MissingConfigurationException(ConfigKeys.SearchHost);
    private string AdminKey => config[ConfigKeys.SearchAdminKey] ?? throw new MissingConfigurationException(ConfigKeys.SearchAdminKey);

    private string GetIndexName<T>() => typeof(T).Name.ToLower();
}
