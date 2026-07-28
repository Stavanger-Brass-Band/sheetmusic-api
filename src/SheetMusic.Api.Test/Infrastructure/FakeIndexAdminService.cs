using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Moq;
using SheetMusic.Api.Parts;
using SheetMusic.Api.Search;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Infrastructure;

/// <summary>
/// In-memory fake of IIndexAdminService for integration tests.
/// Stores indexed documents (<see cref="PartIndexItems"/>) and returns a mocked <see cref="SearchClient"/>
/// whose SearchAsync performs a simple in-memory match over those documents. This allows tests to exercise
/// the full search flow - including the "match found" success path - through the real HTTP endpoints,
/// without depending on a real Azure AI Search service.
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

    /// <summary>
    /// Returns a mocked <see cref="SearchClient"/> (real Azure SDK type, constructed via its public
    /// parameterless constructor - see https://learn.microsoft.com/azure/developer/azure-sdk/unit-testing-mocking
    /// for the pattern) whose SearchAsync&lt;T&gt; is stubbed to search <see cref="PartIndexItems"/> in-memory,
    /// using <see cref="SearchModelFactory"/> to build the response models.
    /// </summary>
    public SearchClient GetQueryClient<T>()
    {
        var mock = new Mock<SearchClient>();

        mock.Setup(c => c.SearchAsync<T>(It.IsAny<string>(), It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string searchText, SearchOptions _, CancellationToken _) => BuildSearchResponse<T>(searchText));

        return mock.Object;
    }

    private Response<SearchResults<T>> BuildSearchResponse<T>(string searchText)
    {
        var matches = FindMatches<T>(searchText);

        var searchResults = matches.Select(match => SearchModelFactory.SearchResult(match, score: 1.0, highlights: null));
        var page = SearchModelFactory.SearchResults(searchResults, (long?)matches.Count, facets: null, coverage: null, rawResponse: null!);

        return Response.FromValue(page, null!);
    }

    /// <summary>
    /// Mimics the fuzzy "AND" query built by <c>SearchForPart.Handler</c>: fragments are joined with
    /// " AND " and a trailing "~" is appended to request a fuzzy match. Only <see cref="PartIndex"/> is
    /// supported today, matching against PartName and Aliases (both SearchableField in the real index).
    /// </summary>
    private List<T> FindMatches<T>(string searchText)
    {
        if (typeof(T) != typeof(PartIndex))
            return new List<T>();

        var terms = searchText
            .TrimEnd('~')
            .Split(" AND ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length == 0)
            return new List<T>();

        var matches = PartIndexItems.Where(part => terms.All(term =>
            Contains(part.PartName, term) || part.Aliases.Any(alias => Contains(alias, term))));

        return matches.Cast<T>().ToList();
    }

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
