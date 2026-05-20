using Azure.Search.Documents;
using SheetMusic.Api.Search;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Infrastructure;

/// <summary>
/// In-memory fake of IIndexAdminService for integration tests.
/// Stores indexed documents and provides a SearchClient backed by no real service.
/// For search tests, use the IndexedItems collection directly.
/// </summary>
public class FakeIndexAdminService : IIndexAdminService
{
    public ConcurrentBag<object> IndexedItems { get; } = new();
    public List<PartIndex> PartIndexItems { get; } = new();
    public bool IndexEnsured { get; private set; }
    public bool IndexCleared { get; private set; }

    public Task ClearIndexAsync<T>()
    {
        IndexCleared = true;
        IndexedItems.Clear();
        PartIndexItems.Clear();
        return Task.CompletedTask;
    }

    public Task EnsureIndexAsync<T>()
    {
        IndexEnsured = true;
        return Task.CompletedTask;
    }

    public Task FillIndexAsync<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            IndexedItems.Add(item!);
            if (item is PartIndex partIndex)
                PartIndexItems.Add(partIndex);
        }
        return Task.CompletedTask;
    }

    public SearchClient GetQueryClient<T>()
    {
        // Return null - the SearchForPart handler will catch the exception.
        // For proper search testing, we test the handler directly with a unit test.
        return null!;
    }
}
