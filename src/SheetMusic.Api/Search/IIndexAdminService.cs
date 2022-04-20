using Microsoft.Azure.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Search;

public interface IIndexAdminService
{
    Task ClearIndexAsync<T>();
    Task EnsureIndexAsync<T>();
    Task FillIndexAsync<T>(IEnumerable<T> items);
    SearchIndexClient GetQueryClient<T>();
}
