using Azure;
using Azure.Search.Documents.Indexes;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using SheetMusic.Api.Configuration;
using SheetMusic.Api.Parts;
using SheetMusic.Api.Search;
using System;
using System.Collections.Generic;
using Xunit;

namespace SheetMusic.Api.Test.Search;

/// <summary>
/// Covers <see cref="IndexAdminService"/>'s index name resolution (issue #236): test and prod must be
/// able to share a single Free-tier Azure AI Search service without one environment's index rebuild
/// (<c>ClearIndexAsync</c> deletes before recreating) deleting the other's index.
/// </summary>
public class IndexAdminServiceTests
{
    [Fact]
    public void GetQueryClient_ResolvesUnprefixedIndexName_WhenPrefixIsNotConfigured()
    {
        var service = BuildService();

        service.GetQueryClient<PartIndex>().IndexName.Should().Be("partindex");
    }

    [Fact]
    public void GetQueryClient_ResolvesPrefixedIndexName_WhenPrefixIsConfigured()
    {
        var service = BuildService(indexPrefix: "test");

        service.GetQueryClient<PartIndex>().IndexName.Should().Be("test-partindex");
    }

    private static IndexAdminService BuildService(string? indexPrefix = null)
    {
        var values = new Dictionary<string, string?>();

        if (indexPrefix is not null)
        {
            values[ConfigKeys.SearchIndexPrefix] = indexPrefix;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var searchIndexClient = new SearchIndexClient(new Uri("https://example.search.windows.net"), new AzureKeyCredential("test-admin-key"));
        return new IndexAdminService(searchIndexClient, configuration);
    }
}
