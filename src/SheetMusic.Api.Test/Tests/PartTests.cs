using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Utility;
using System;
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
}
