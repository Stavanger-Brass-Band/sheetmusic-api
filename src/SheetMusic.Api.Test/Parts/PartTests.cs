using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts.ViewModels;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Parts;

[Collection(Collections.Part)]
public class PartTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task CreatePart_ShouldBeForbidden_WhenMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync($"parts", new { Name = "Test", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePart_ShouldReturn401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"parts", new { Name = "Test", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePart_ShouldReturnBadRequest_WhenNameMissing()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PostAsJsonAsync("parts", new { Name = "", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePart_ShouldBeSuccessfull_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
    }

    [Fact]
    public async Task GetPart_ShouldBeSuccessfull_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var response = await adminClient.GetAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPart_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPart_ShouldReturnPart_WhenFoundByAlias()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=the-alias", new { });

        var response = await adminClient.GetAsync("parts/the-alias");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);
        result!.Id.Should().Be(part.Id);
    }

    [Fact]
    public async Task UpdatePart_ShouldBeSuccessfull_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var input = partBuilder.GetPartInput(part.Name);

        if (input is null)
            throw new Exception("Input model not found for newly created entity");

        input.Name = "changed";

        var response = await adminClient.PutAsJsonAsync($"parts/{part.Id}", input);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatePart_ShouldBeForbidden_WhenMusikant()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.PutAsJsonAsync($"parts/{part.Id}", new { Name = "changed", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePart_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync($"parts/{part.Id}", new { Name = "changed", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddAlias_ShouldAddSuccessfully()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var response = await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=testing", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddAlias_ShouldBeForbidden_WhenMusikant()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=testing", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddAlias_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=testing", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveAlias_ShouldRemoveSuccessfully()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var response = await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=testing", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response = await adminClient.DeleteAsync($"parts/{part.Id}/aliases/testing");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveAlias_ShouldBeForbidden_WhenMusikant()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=testing", new { });

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"parts/{part.Id}/aliases/testing");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePart_ShouldDeleteSuccessfully()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var response = await adminClient.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePart_ShouldBeForbidden_WhenMusikant()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePart_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePart_ShouldReturnConflict_WhenPartIsUsedInSet()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        var set = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var path = $"{Path.GetTempPath()}{part.Name}.pdf";
        await File.WriteAllTextAsync(path, "content");
        await FileUploader.UploadOneFile(path, adminClient, $"sheetmusic/sets/{set.Id}/parts/{part.Name}/content?api-version=2.0");

        var response = await adminClient.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeletePart_ShouldReturnConflict_WhenPartIsAssignedToMusician()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            var musician = new Musician { Id = Guid.NewGuid(), Name = $"Musician-{Guid.NewGuid()}" };
            db.Musicians.Add(musician);
            db.Set<MusicianMusicPart>().Add(new MusicianMusicPart { Id = Guid.NewGuid(), MusicianId = musician.Id, MusicPartId = part.Id });
            await db.SaveChangesAsync();
        }

        var response = await adminClient.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SearchForPart_ShouldReturn404_WhenNoMatch()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("parts/index?searchTerm=nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchForPart_ShouldReturnMatch_WhenPartNameMatches()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partName = $"searchable-part-{Guid.NewGuid():N}";
        var createResponse = await adminClient.PostAsJsonAsync("parts", new { Name = partName, SortOrder = 1, Indexable = true });
        var part = await createResponse.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);

        var buildResponse = await adminClient.PostAsync("parts/index", null);
        buildResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await adminClient.GetAsync($"parts/index?searchTerm={partName}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);
        result!.Id.Should().Be(part!.Id);
        result.Name.Should().Be(partName);
    }

    [Fact]
    public async Task SearchForPart_ShouldReturnMatch_WhenSearchingByAlias()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partName = $"aliased-part-{Guid.NewGuid():N}";
        var createResponse = await adminClient.PostAsJsonAsync("parts", new { Name = partName, SortOrder = 1, Indexable = true });
        var part = await createResponse.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);

        await adminClient.PostAsJsonAsync($"parts/{part!.Id}/aliases?alias=the-search-alias", new { });

        var buildResponse = await adminClient.PostAsync("parts/index", null);
        buildResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await adminClient.GetAsync("parts/index?searchTerm=the-search-alias");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);
        result!.Id.Should().Be(part.Id);
    }

    [Fact]
    public async Task SearchForPart_ShouldReturn404_WhenPartExistsButIsNotIndexable()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var input = partBuilder.GetPartInput(part.Name);
        if (input is null)
            throw new Exception("Input model not found for newly created entity");

        input.Indexable = false;
        await adminClient.PutAsJsonAsync($"parts/{part.Id}", input); // triggers an index rebuild

        var response = await adminClient.GetAsync($"parts/index?searchTerm={part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchForPart_ShouldBeForbidden_WhenMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("parts/index?searchTerm=test");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPartList_ShouldReturnAllParts_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        await partBuilder.WithParts(3).ProvisionAsync();

        var response = await adminClient.GetAsync("parts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts.Should().NotBeNull();
        parts!.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetPartList_ShouldBeForbidden_WhenMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("parts");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetPartList_WithExpandAliases_ShouldIncludeAliases()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        var alias = $"expand-alias-{Guid.NewGuid():N}";

        await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias={alias}", new { });

        var response = await adminClient.GetAsync($"parts?$filter={Uri.EscapeDataString($"name eq '{part.Name}'")}&$expand=aliases");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts.Should().ContainSingle();
        parts![0].Aliases.Should().Contain(alias);
    }

    [Fact]
    public async Task GetPartList_WithoutExpand_ShouldNotIncludeAliases()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        var alias = $"no-expand-alias-{Guid.NewGuid():N}";

        await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias={alias}", new { });

        var response = await adminClient.GetAsync($"parts?$filter={Uri.EscapeDataString($"name eq '{part.Name}'")}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts.Should().ContainSingle();
        parts![0].Aliases.Should().BeEmpty();
    }

    [Theory]
    [InlineData("$expand=parts")]
    [InlineData("$expand=aliases,unknown")]
    [InlineData("$expand=aliases,")]
    [InlineData("$expand=")]
    public async Task GetPartList_WithInvalidExpand_ShouldReturnBadRequest(string clause)
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync($"parts?{clause}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPart_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("parts/nonexistent-part-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPart_ShouldReturnPart_WhenFoundById()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var response = await adminClient.GetAsync($"parts/{part.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);
        result!.Id.Should().Be(part.Id);
        result.Name.Should().Be(part.Name);
    }

    [Fact]
    public async Task UpdatePart_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PutAsJsonAsync("parts/nonexistent-part-xyz", new { Name = "test", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePart_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.DeleteAsync("parts/nonexistent-part-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddAlias_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PostAsJsonAsync("parts/nonexistent-part-xyz/aliases?alias=test", new { });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddAlias_ShouldReturnConflict_WhenAliasAlreadyExists()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=duplicate-alias", new { });
        var response = await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=duplicate-alias", new { });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteAlias_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.DeleteAsync("parts/nonexistent-part-xyz/aliases/test");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BuildPartIndex_ShouldBeSuccessful_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var response = await adminClient.PostAsync("parts/index", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task BuildPartIndex_ShouldBeForbidden_WhenMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsync("parts/index", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BuildPartIndex_ShouldBeForbidden_WhenNoteansvarlig()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.PostAsync("parts/index", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreatePart_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.PostAsJsonAsync("parts", new { Name = $"noteansvarlig-part-{Guid.NewGuid():N}", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPartList_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.GetAsync("parts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePart_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);
        var part = await new PartDataBuilder(client).ProvisionSinglePartAsync();

        var response = await client.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #region OData $orderby, $skip and $top

    private static async Task SeedPartsAsync(HttpClient client, params string[] names)
    {
        foreach (var name in names)
            await client.PostAsJsonAsync("parts", new { Name = name, SortOrder = 1, Indexable = false });
    }

    [Fact]
    public async Task GetPartList_ShouldRespectOrderBy_WhenOrderByAscendingProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedPartsAsync(client, "Zulu part", "Alfa part", "Mike part");

        var response = await client.GetAsync("parts?$orderby=name asc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts!.Select(p => p.Name).Should().Equal("Alfa part", "Mike part", "Zulu part");
    }

    [Fact]
    public async Task GetPartList_ShouldRespectOrderBy_WhenOrderByDescendingProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedPartsAsync(client, "Zulu part", "Alfa part", "Mike part");

        var response = await client.GetAsync("parts?$orderby=name desc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts!.Select(p => p.Name).Should().Equal("Zulu part", "Mike part", "Alfa part");
    }

    [Fact]
    public async Task GetPartList_ShouldRespectTop_WhenTopProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedPartsAsync(client, "Alfa part", "Bravo part", "Charlie part");

        var response = await client.GetAsync("parts?$orderby=name asc&$top=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts!.Select(p => p.Name).Should().Equal("Alfa part", "Bravo part");
    }

    [Fact]
    public async Task GetPartList_ShouldRespectSkipAndTop_WhenBothProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedPartsAsync(client, "Alfa part", "Bravo part", "Charlie part", "Delta part");

        var response = await client.GetAsync("parts?$orderby=name asc&$skip=1&$top=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var parts = await response.Content.ReadFromJsonAsync<List<ApiPart>>(JsonDefaults.Options);
        parts!.Select(p => p.Name).Should().Equal("Bravo part", "Charlie part");
    }

    #endregion
}
