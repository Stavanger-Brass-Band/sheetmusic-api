using FluentAssertions;
using Newtonsoft.Json;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Models;
using SheetMusic.Api.Test.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests;

[Collection(Collections.Part)]
public class PartTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task CreatePart_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync($"parts", new { Name = "Test", SortOrder = 1, Indexable = false });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
    public async Task AddAlias_ShouldAddSuccessfully()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var response = await adminClient.PostAsJsonAsync($"parts/{part.Id}/aliases?alias=testing", new { });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
    public async Task DeletePart_ShouldDeleteSuccessfully()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var partBuilder = new PartDataBuilder(adminClient);
        var part = await partBuilder.ProvisionSinglePartAsync();

        var response = await adminClient.DeleteAsync($"parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SearchForPart_ShouldReturn404_WhenNoMatch()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("parts/index?searchTerm=nonexistent");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchForPart_ShouldBeForbidden_WhenReader()
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

        var body = await response.Content.ReadAsStringAsync();
        var parts = JsonConvert.DeserializeObject<List<ApiPart>>(body);
        parts.Should().NotBeNull();
        parts!.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task GetPartList_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("parts");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiPart>(body);
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
    public async Task BuildPartIndex_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsync("parts/index", null);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
