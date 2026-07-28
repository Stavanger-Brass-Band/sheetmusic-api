using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Sets.Models;
using SheetMusic.Api.Test.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Sets;

/// <summary>
/// Full coverage of <c>GET /sheetmusic/sets</c>. Runs in its own collection so the in-memory
/// database is isolated from other test classes.
/// </summary>
[Collection(Collections.SetList)]
public class GetSetListTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    private const string Marker = "SetListCorpus";

    private static readonly SemaphoreSlim CorpusLock = new(1, 1);
    private static List<ApiSet>? corpus;

    /// <summary>
    /// Insertion order deliberately differs from alphabetical title order, so ordering tests
    /// cannot pass by accident on the default archive number ordering.
    /// </summary>
    private static readonly CorpusSpec[] CorpusSpecs =
    [
        new("Hotel", 2, 1, 1),
        new("Charlie", 1, 3, 2),
        new("Alfa", 3, 2, 1),
        new("Golf", 1, 1, 3),
        new("Delta", 2, 3, 2),
        new("Foxtrot", 3, 2, 3),
        new("Bravo", 1, 1, 1),
        new("Echo", 2, 3, 2)
    ];

    #region Baseline

    [Fact]
    public async Task GetSetList_ShouldReturnAllSetsOrderedByArchiveNumberAscending_WhenNoQueryParams()
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, "");

        items.Should().BeInAscendingOrder(s => s.ArchiveNumber);

        foreach (var set in seeded)
            items.Should().Contain(s => s.Id == set.Id && s.Title == set.Title);
    }

    [Fact]
    public async Task GetSetList_ShouldReturnEmptyArray_WhenDatabaseIsEmpty()
    {
        using var emptyFactory = new SheetMusicWebAppFactory();
        var client = emptyFactory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, "");

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSetList_ShouldReturn401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("sheetmusic/sets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSetList_ShouldPopulateZipDownloadUrlAndPartsUrl()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(Marker));

        items.Should().NotBeEmpty();

        foreach (var item in items)
        {
            Uri.IsWellFormedUriString(item.ZipDownloadUrl, UriKind.Absolute).Should().BeTrue();
            Uri.IsWellFormedUriString(item.PartsUrl, UriKind.Absolute).Should().BeTrue();
            item.ZipDownloadUrl.Should().EndWith($"/sheetmusic/sets/{item.Id}/zip");
            item.PartsUrl.Should().EndWith($"/sheetmusic/sets/{item.Id}/parts");
        }
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    public async Task GetSetList_ShouldBeSuccessful_ForEveryApiVersion(string apiVersion)
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&api-version={apiVersion}");

        items.Should().HaveCount(seeded.Count);
    }

    #endregion

    #region $search

    [Fact]
    public async Task GetSetList_WithSearchOnTitle_ShouldReturnMatchingSet()
    {
        var seeded = await GetCorpusAsync();
        var alfa = CorpusSet(seeded, "Alfa");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(alfa.Title!));

        items.Should().ContainSingle();
        items[0].Id.Should().Be(alfa.Id);
    }

    [Fact]
    public async Task GetSetList_WithSearchOnComposer_ShouldReturnMatchingSets()
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(Composer(3)));

        items.Select(i => i.Title).Should().BeEquivalentTo([TitleOf("Alfa"), TitleOf("Foxtrot")]);
    }

    [Fact]
    public async Task GetSetList_WithSearchOnArranger_ShouldReturnMatchingSets()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(Arranger(2)));

        items.Select(i => i.Title).Should().BeEquivalentTo([TitleOf("Alfa"), TitleOf("Foxtrot")]);
    }

    [Fact]
    public async Task GetSetList_WithSearchOnArchiveNumber_ShouldReturnMatchingSet()
    {
        var seeded = await GetCorpusAsync();
        var echo = CorpusSet(seeded, "Echo");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(echo.ArchiveNumber.ToString()));

        items.Should().Contain(s => s.Id == echo.Id);
    }

    [Fact]
    public async Task GetSetList_WithSearchWithoutMatches_ShouldReturnEmptyArray()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search("no-set-will-ever-match-this-term"));

        items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSetList_WithBlankSearch_ShouldBeTreatedAsNoSearch(string term)
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(term));

        foreach (var set in seeded)
            items.Should().Contain(s => s.Id == set.Id);
    }

    [Theory]
    [InlineData("%'\"()[]*?<>&#\\/;:=+")]
    [InlineData("Ærlig Øst Åse")]
    public async Task GetSetList_WithSpecialCharactersInSearch_ShouldNotThrow(string term)
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"sheetmusic/sets{Search(term)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSetList_WithVeryLongSearchTerm_ShouldNotThrow()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"sheetmusic/sets{Search(new string('a', 2000))}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region $filter

    [Fact]
    public async Task GetSetList_WithFilterEqOnArchiveNumber_ShouldReturnMatchingSet()
    {
        var seeded = await GetCorpusAsync();
        var delta = CorpusSet(seeded, "Delta");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber eq {delta.ArchiveNumber}"));

        items.Should().ContainSingle();
        items[0].Id.Should().Be(delta.Id);
    }

    [Fact]
    public async Task GetSetList_WithFilterEqOnTitle_ShouldReturnMatchingSet()
    {
        var seeded = await GetCorpusAsync();
        var golf = CorpusSet(seeded, "Golf");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"title eq '{golf.Title}'"));

        items.Should().ContainSingle();
        items[0].Id.Should().Be(golf.Id);
    }

    [Fact]
    public async Task GetSetList_WithFilterEqOnComposer_ShouldReturnMatchingSets()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"composer eq '{Composer(1)}'"));

        items.Select(i => i.Title).Should().BeEquivalentTo([TitleOf("Charlie"), TitleOf("Golf"), TitleOf("Bravo")]);
    }

    [Fact]
    public async Task GetSetList_WithFilterEqOnArranger_ShouldReturnMatchingSets()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"arranger eq '{Arranger(2)}'"));

        items.Select(i => i.Title).Should().BeEquivalentTo([TitleOf("Alfa"), TitleOf("Foxtrot")]);
    }

    [Fact]
    public async Task GetSetList_WithFilterEqOnSoleSellingAgent_ShouldReturnMatchingSets()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"soleSellingAgent eq '{Agent(3)}'"));

        items.Select(i => i.Title).Should().BeEquivalentTo([TitleOf("Golf"), TitleOf("Foxtrot")]);
    }

    [Fact]
    public async Task GetSetList_WithFilterNeOnArchiveNumber_ShouldExcludeMatchingSet()
    {
        var seeded = await GetCorpusAsync();
        var hotel = CorpusSet(seeded, "Hotel");
        var charlie = CorpusSet(seeded, "Charlie");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber ne {hotel.ArchiveNumber}"));

        items.Should().NotContain(s => s.Id == hotel.Id);
        items.Should().Contain(s => s.Id == charlie.Id);
    }

    [Fact]
    public async Task GetSetList_WithFilterGtOnArchiveNumber_ShouldReturnHigherNumbersOnly()
    {
        var seeded = await GetCorpusAsync();
        var alfa = CorpusSet(seeded, "Alfa");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber gt {alfa.ArchiveNumber}"));

        items.Should().OnlyContain(s => s.ArchiveNumber > alfa.ArchiveNumber);
        items.Should().Contain(s => s.Id == CorpusSet(seeded, "Golf").Id);
    }

    [Fact]
    public async Task GetSetList_WithFilterLtOnArchiveNumber_ShouldReturnLowerNumbersOnly()
    {
        var seeded = await GetCorpusAsync();
        var echo = CorpusSet(seeded, "Echo");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber lt {echo.ArchiveNumber}"));

        items.Should().OnlyContain(s => s.ArchiveNumber < echo.ArchiveNumber);
        items.Should().Contain(s => s.Id == CorpusSet(seeded, "Hotel").Id);
    }

    [Fact]
    public async Task GetSetList_WithFilterGeOnArchiveNumber_ShouldIncludeBoundary()
    {
        var seeded = await GetCorpusAsync();
        var alfa = CorpusSet(seeded, "Alfa");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber ge {alfa.ArchiveNumber}"));

        items.Should().OnlyContain(s => s.ArchiveNumber >= alfa.ArchiveNumber);
        items.Should().Contain(s => s.Id == alfa.Id);
    }

    [Fact]
    public async Task GetSetList_WithFilterLeOnArchiveNumber_ShouldIncludeBoundary()
    {
        var seeded = await GetCorpusAsync();
        var echo = CorpusSet(seeded, "Echo");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber le {echo.ArchiveNumber}"));

        items.Should().OnlyContain(s => s.ArchiveNumber <= echo.ArchiveNumber);
        items.Should().Contain(s => s.Id == echo.Id);
    }

    [Fact]
    public async Task GetSetList_WithAndFilterGroup_ShouldReturnIntersection()
    {
        var seeded = await GetCorpusAsync();
        var alfa = CorpusSet(seeded, "Alfa");
        var delta = CorpusSet(seeded, "Delta");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"archiveNumber ge {alfa.ArchiveNumber} and archiveNumber le {delta.ArchiveNumber}"));

        items.Should().OnlyContain(s => s.ArchiveNumber >= alfa.ArchiveNumber && s.ArchiveNumber <= delta.ArchiveNumber);
        items.Should().Contain(s => s.Id == alfa.Id);
        items.Should().Contain(s => s.Id == delta.Id);
    }

    [Fact]
    public async Task GetSetList_WithOrFilterGroup_ShouldReturnUnion()
    {
        var seeded = await GetCorpusAsync();
        var bravo = CorpusSet(seeded, "Bravo");
        var echo = CorpusSet(seeded, "Echo");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"title eq '{bravo.Title}' or title eq '{echo.Title}'"));

        items.Select(i => i.Id).Should().BeEquivalentTo([bravo.Id, echo.Id]);
    }

    [Fact]
    public async Task GetSetList_WithInFilter_ShouldReturnAllListedValues()
    {
        var seeded = await GetCorpusAsync();
        var charlie = CorpusSet(seeded, "Charlie");
        var foxtrot = CorpusSet(seeded, "Foxtrot");
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Filter($"title in ('{charlie.Title}','{foxtrot.Title}')"));

        items.Select(i => i.Id).Should().BeEquivalentTo([charlie.Id, foxtrot.Id]);
    }

    [Theory]
    [InlineData("foo eq 'bar'")]
    [InlineData("title")]
    [InlineData("title eq")]
    [InlineData("title like 'something'")]
    [InlineData("archiveNumber eq 'notanumber'")]
    [InlineData("(((")]
    public async Task GetSetList_WithInvalidFilter_ShouldReturnBadRequestWithProblemDetails(string filter)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        await AssertBadRequestAsync(client, Filter(filter));
    }

    [Fact]
    public async Task GetSetList_WithEmptyFilter_ShouldReturnBadRequestWithProblemDetails()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        await AssertBadRequestAsync(client, "?$filter=");
    }

    #endregion

    #region $orderby

    [Theory]
    [InlineData("title")]
    [InlineData("composer")]
    [InlineData("arranger")]
    [InlineData("soleSellingAgent")]
    [InlineData("archiveNumber")]
    public async Task GetSetList_WithOrderByAscending_ShouldReturnSetsInAscendingOrder(string field)
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&$orderby={field} asc");

        items.Should().HaveCount(seeded.Count);
        items.Select(i => SortKey(i, field)).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("composer")]
    [InlineData("arranger")]
    [InlineData("soleSellingAgent")]
    [InlineData("archiveNumber")]
    public async Task GetSetList_WithOrderByDescending_ShouldReturnSetsInDescendingOrder(string field)
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&$orderby={field} desc");

        items.Should().HaveCount(seeded.Count);
        items.Select(i => SortKey(i, field)).Should().BeInDescendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetSetList_WithOrderByWithoutDirection_ShouldSortAscending()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&$orderby=title");

        items.Select(i => i.Title).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetSetList_WithMultipleOrderByClauses_ShouldApplyThemInSequence()
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&$orderby=composer asc,title desc");

        var expected = seeded
            .OrderBy(s => s.Composer, StringComparer.Ordinal)
            .ThenByDescending(s => s.Title, StringComparer.Ordinal)
            .Select(s => s.Title)
            .ToList();

        items.Select(i => i.Title).Should().ContainInOrder(expected);
    }

    [Fact]
    public async Task GetSetList_WithoutOrderBy_ShouldUseArchiveNumberAscendingAsDefault()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(Marker));

        items.Should().BeInAscendingOrder(s => s.ArchiveNumber);
    }

    [Theory]
    [InlineData("$orderby=foo")]
    [InlineData("$orderby=title sideways")]
    [InlineData("$orderby=title asc extra")]
    [InlineData("$orderby=")]
    public async Task GetSetList_WithInvalidOrderBy_ShouldReturnBadRequestWithProblemDetails(string clause)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        await AssertBadRequestAsync(client, $"?{clause}");
    }

    /// <summary>
    /// <c>$orderby</c> is OData syntax, never JSON. A serialized sort option contains commas, so without
    /// field name validation it was split into several fragments that were happily accepted as sort clauses.
    /// </summary>
    [Theory]
    [InlineData("""[{"field":"title","direction":0}]""")]
    [InlineData("""{"field":"title","direction":0}""")]
    public async Task GetSetList_WithJsonSerialisedOrderBy_ShouldReturnBadRequestWithProblemDetails(string json)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var problem = await AssertBadRequestAsync(client, $"?$orderby={Uri.EscapeDataString(json)}");

        problem.Detail.Should().Contain("$orderby", "the JSON must be rejected while parsing, not by the field mapping");
        problem.Detail.Should().NotContain("mapping");
    }

    #endregion

    #region $top and $skip

    [Fact]
    public async Task GetSetList_WithTop_ShouldLimitResultCount()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&$top=3");

        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSetList_WithSkip_ShouldOffsetResults()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var all = await GetSetsAsync(client, Search(Marker));
        var skipped = await GetSetsAsync(client, $"{Search(Marker)}&$skip=2");

        skipped.Select(s => s.Id).Should().Equal(all.Skip(2).Select(s => s.Id));
    }

    [Fact]
    public async Task GetSetList_WithTopAndSkip_ShouldPageCorrectly()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var all = await GetSetsAsync(client, Search(Marker));
        var page = await GetSetsAsync(client, $"{Search(Marker)}&$skip=2&$top=3");

        page.Select(s => s.Id).Should().Equal(all.Skip(2).Take(3).Select(s => s.Id));
    }

    [Fact]
    public async Task GetSetList_WithSkipBeyondResultCount_ShouldReturnEmptyArray()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&$skip=100000");

        items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("$top=0")]
    [InlineData("$top=-1")]
    [InlineData("$top=notanumber")]
    [InlineData("$top=")]
    [InlineData("$skip=-1")]
    [InlineData("$skip=notanumber")]
    [InlineData("$skip=")]
    public async Task GetSetList_WithInvalidPaging_ShouldReturnBadRequestWithProblemDetails(string clause)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        await AssertBadRequestAsync(client, $"?{clause}");
    }

    #endregion

    #region $expand

    [Fact]
    public async Task GetSetList_WithExpandParts_ShouldIncludePartsWithDownloadUrls()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var title = $"ExpandTest-{Guid.NewGuid()}";
        var set = await CreateSetAsync(adminClient, title);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(adminClient, set, part.Name);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var items = await GetSetsAsync(client, $"{Search(title)}&$expand=parts");

        items.Should().ContainSingle();
        items[0].Parts.Should().NotBeNull();
        items[0].Parts.Should().ContainSingle();

        var setPart = items[0].Parts![0];
        setPart.PdfDownloadUrl.Should().EndWith($"/sheetmusic/sets/{set.Id}/parts/{setPart.MusicPartId}/pdf");
        setPart.DeletePartUrl.Should().EndWith($"/sheetmusic/sets/{set.Id}/parts/{setPart.MusicPartId}");
    }

    [Fact]
    public async Task GetSetList_WithoutExpand_ShouldLeavePartsNull()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, Search(Marker));

        items.Should().OnlyContain(s => s.Parts == null);
    }

    [Theory]
    [InlineData("$expand=categories")]
    [InlineData("$expand=parts,unknown")]
    [InlineData("$expand=parts,")]
    [InlineData("$expand=")]
    public async Task GetSetList_WithInvalidExpand_ShouldReturnBadRequestWithProblemDetails(string clause)
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        await AssertBadRequestAsync(client, $"?{clause}");
    }

    #endregion

    #region category

    [Fact]
    public async Task GetSetList_FilteredByCategoryName_ShouldReturnOnlyMatchingSets()
    {
        var seeded = await GetCorpusAsync();
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var alfa = CorpusSet(seeded, "Alfa");
        var bravo = CorpusSet(seeded, "Bravo");
        var category = await CreateCategoryAsync(adminClient);

        await AssignCategoryAsync(adminClient, alfa, category);
        await AssignCategoryAsync(adminClient, bravo, category);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var items = await GetSetsAsync(client, $"?category={Uri.EscapeDataString(category.Name!)}");

        items.Select(i => i.Id).Should().BeEquivalentTo([alfa.Id, bravo.Id]);
    }

    [Fact]
    public async Task GetSetList_FilteredByCategoryId_ShouldReturnOnlyMatchingSets()
    {
        var seeded = await GetCorpusAsync();
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var delta = CorpusSet(seeded, "Delta");
        var category = await CreateCategoryAsync(adminClient);

        await AssignCategoryAsync(adminClient, delta, category);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var items = await GetSetsAsync(client, $"?category={category.Id}");

        items.Select(i => i.Id).Should().BeEquivalentTo([delta.Id]);
    }

    [Fact]
    public async Task GetSetList_FilteredByUnknownCategory_ShouldReturn404WithProblemDetails()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("sheetmusic/sets?category=no-such-category-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonDefaults.Options);
        problem.Should().NotBeNull();
        problem!.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetSetList_FilteredByCategory_ShouldHonourSearchFilterOrderByAndPaging()
    {
        var seeded = await GetCorpusAsync();
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var category = await CreateCategoryAsync(adminClient);

        var categorized = new[] { "Charlie", "Golf", "Bravo" }.Select(n => CorpusSet(seeded, n)).ToList();
        foreach (var set in categorized)
            await AssignCategoryAsync(adminClient, set, category);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var searched = await GetSetsAsync(client, $"?category={category.Id}&{SearchValue(Marker)}");
        searched.Select(i => i.Id).Should().BeEquivalentTo(categorized.Select(s => s.Id));

        var filtered = await GetSetsAsync(client, $"?category={category.Id}&{FilterValue($"composer eq '{Composer(1)}'")}");
        filtered.Select(i => i.Id).Should().BeEquivalentTo(categorized.Select(s => s.Id));

        var ordered = await GetSetsAsync(client, $"?category={category.Id}&$orderby=title desc");
        ordered.Select(i => i.Title).Should().Equal([TitleOf("Golf"), TitleOf("Charlie"), TitleOf("Bravo")]);

        var paged = await GetSetsAsync(client, $"?category={category.Id}&$orderby=title asc&$skip=1&$top=1");
        paged.Select(i => i.Title).Should().Equal([TitleOf("Charlie")]);
    }

    #endregion

    #region combinations

    [Fact]
    public async Task GetSetList_WithSearchAndFilter_ShouldApplyBoth()
    {
        var seeded = await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&{FilterValue($"title eq '{CorpusSet(seeded, "Golf").Title}'")}");

        items.Should().ContainSingle();
        items[0].Title.Should().Be(TitleOf("Golf"));
    }

    [Fact]
    public async Task GetSetList_WithSearchAndNonMatchingFilter_ShouldReturnEmptyArray()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var items = await GetSetsAsync(client, $"{Search(Marker)}&{FilterValue("title eq 'a title no set has'")}");

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSetList_WithAllQueryOptionsCombined_ShouldApplyThemTogether()
    {
        await GetCorpusAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var query = $"{Search(Marker)}&{FilterValue($"composer eq '{Composer(1)}'")}&$orderby=title asc&$skip=1&$top=2&$expand=parts";
        var items = await GetSetsAsync(client, query);

        items.Select(i => i.Title).Should().Equal([TitleOf("Charlie"), TitleOf("Golf")]);
        items.Should().OnlyContain(s => s.Parts != null);
    }

    #endregion

    #region helpers

    private sealed record CorpusSpec(string Name, int ComposerIndex, int ArrangerIndex, int AgentIndex);

    private sealed record ProblemResponse(int? Status, string? Title, string? Type, string? Detail);

    private static string TitleOf(string name) => $"{Marker} {name}";

    private static string Composer(int index) => $"{Marker}Composer{index}";

    private static string Arranger(int index) => $"{Marker}Arranger{index}";

    private static string Agent(int index) => $"{Marker}Agent{index}";

    private static ApiSet CorpusSet(List<ApiSet> sets, string name) => sets.Single(s => s.Title == TitleOf(name));

    private static string Search(string term) => $"?{SearchValue(term)}";

    private static string SearchValue(string term) => $"$search={Uri.EscapeDataString(term)}";

    private static string Filter(string filter) => $"?{FilterValue(filter)}";

    private static string FilterValue(string filter) => $"$filter={Uri.EscapeDataString(filter)}";

    private static string? SortKey(ApiSet set, string field) => field switch
    {
        "title" => set.Title,
        "composer" => set.Composer,
        "arranger" => set.Arranger,
        "soleSellingAgent" => set.SoleSellingAgent,
        "archiveNumber" => set.ArchiveNumber.ToString("D9"),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unmapped sort field")
    };

    private static async Task<List<ApiSet>> GetSetsAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"sheetmusic/sets{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        items.Should().NotBeNull();

        return items!;
    }

    private static async Task<ProblemResponse> AssertBadRequestAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"sheetmusic/sets{query}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "invalid consumer input must never produce a 500");

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonDefaults.Options);
        problem.Should().NotBeNull();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
        problem.Detail.Should().NotBeNullOrWhiteSpace();

        return problem;
    }

    private async Task<List<ApiSet>> GetCorpusAsync()
    {
        if (corpus is not null)
            return corpus;

        await CorpusLock.WaitAsync();

        try
        {
            if (corpus is not null)
                return corpus;

            var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
            var created = new List<ApiSet>();

            foreach (var spec in CorpusSpecs)
            {
                created.Add(await CreateSetAsync(
                    adminClient,
                    TitleOf(spec.Name),
                    Composer(spec.ComposerIndex),
                    Arranger(spec.ArrangerIndex),
                    Agent(spec.AgentIndex)));
            }

            corpus = created;
            return corpus;
        }
        finally
        {
            CorpusLock.Release();
        }
    }

    private static async Task<ApiSet> CreateSetAsync(HttpClient adminClient, string title, string? composer = null, string? arranger = null, string? soleSellingAgent = null)
    {
        var response = await adminClient.PostAsJsonAsync("sheetmusic/sets", new
        {
            Title = title,
            Composer = composer,
            Arranger = arranger,
            SoleSellingAgent = soleSellingAgent
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var set = await response.Content.ReadFromJsonAsync<ApiSet>(JsonDefaults.Options);
        set.Should().NotBeNull();

        return set!;
    }

    private static async Task AddPartToSetAsync(HttpClient adminClient, ApiSet set, string partName)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{partName}.pdf");
        await File.WriteAllTextAsync(path, "not-a-real-pdf");

        await FileUploader.UploadOneFile(path, adminClient, $"sheetmusic/sets/{set.Id}/parts/{partName}/content?api-version=2.0");
    }

    private static async Task<ApiCategory> CreateCategoryAsync(HttpClient adminClient)
    {
        var response = await adminClient.PostAsJsonAsync("categories", new { Name = $"SetListCategory-{Guid.NewGuid()}", Inactive = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var category = await response.Content.ReadFromJsonAsync<ApiCategory>(JsonDefaults.Options);
        category.Should().NotBeNull();

        return category!;
    }

    private static async Task AssignCategoryAsync(HttpClient adminClient, ApiSet set, ApiCategory category)
    {
        var response = await adminClient.PostAsJsonAsync($"sheetmusic/sets/{set.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #endregion
}
