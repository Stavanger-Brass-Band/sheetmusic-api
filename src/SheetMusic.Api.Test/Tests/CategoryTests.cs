using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Models;
using SheetMusic.Api.Test.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests;

[Collection(Collections.Set)]
public class CategoryTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task GetCategoryList_ShouldReturnSeededCategory()
    {
        var category = await SeedCategoryAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("categories");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<ApiCategory>>(JsonDefaults.Options);
        items.Should().Contain(c => c.Id == category.Id && c.Name == category.Name);
    }

    [Fact]
    public async Task GetCategory_ShouldReturnCategory_WhenLookedUpById()
    {
        var category = await SeedCategoryAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiCategory>(JsonDefaults.Options);
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be(category.Name);
        result.Inactive.Should().Be(category.Inactive);
    }

    [Fact]
    public async Task GetCategory_ShouldReturnCategory_WhenLookedUpByName()
    {
        var category = await SeedCategoryAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"categories/{category.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiCategory>(JsonDefaults.Options);
        result.Should().NotBeNull();
        result!.Id.Should().Be(category.Id);
        result.Name.Should().Be(category.Name);
    }

    [Fact]
    public async Task GetCategory_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("categories/nonexistent-category-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignCategoryToSet_ShouldBeSuccessful()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        var response = await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<ApiCategory>>(JsonDefaults.Options);
        categories.Should().Contain(c => c.Id == category.Id);

        var getResponse = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/categories");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedCategories = await getResponse.Content.ReadFromJsonAsync<List<ApiCategory>>(JsonDefaults.Options);
        fetchedCategories.Should().Contain(c => c.Id == category.Id && c.Name == category.Name);

        var setResponse = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}");
        var apiSet = await setResponse.Content.ReadFromJsonAsync<ApiSet>(JsonDefaults.Options);
        apiSet!.Categories.Should().Contain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task AssignCategoryToSet_ShouldBeForbidden_ForReaderUser()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignCategoryToSet_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var category = await SeedCategoryAsync();

        var response = await adminClient.PostAsJsonAsync("sheetmusic/sets/nonexistent-set-xyz/categories", new { CategoryIdentifier = category.Id.ToString() });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignCategoryToSet_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var response = await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = "nonexistent-category-xyz" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignCategoryToSet_ShouldReturnConflict_WhenAlreadyAssigned()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        var firstResponse = await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondResponse = await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RemoveCategoryFromSet_ShouldBeSuccessful()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });

        var response = await adminClient.DeleteAsync($"sheetmusic/sets/{testSet.Id}/categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/categories");
        var categories = await getResponse.Content.ReadFromJsonAsync<List<ApiCategory>>(JsonDefaults.Options);
        categories.Should().NotContain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task RemoveCategoryFromSet_ShouldReturn404_WhenNotAssigned()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        var response = await adminClient.DeleteAsync($"sheetmusic/sets/{testSet.Id}/categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveCategoryFromSet_ShouldBeForbidden_ForReaderUser()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();
        await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"sheetmusic/sets/{testSet.Id}/categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSetList_FilteredByCategory_ShouldReturnOnlyMatchingSets()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var matchingSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var otherSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        await adminClient.PostAsJsonAsync($"sheetmusic/sets/{matchingSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.GetAsync($"sheetmusic/sets?category={category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        items.Should().Contain(s => s.Id == matchingSet.Id);
        items.Should().NotContain(s => s.Id == otherSet.Id);
    }

    [Fact]
    public async Task GetSetList_FilteredByCategory_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("sheetmusic/sets?category=nonexistent-category-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddNewCategory_ShouldBeSuccessful()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var name = $"Category-{Guid.NewGuid()}";

        var response = await adminClient.PostAsJsonAsync("categories", new { Name = name, Inactive = false });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var category = await response.Content.ReadFromJsonAsync<ApiCategory>(JsonDefaults.Options);
        category.Should().NotBeNull();
        category!.Name.Should().Be(name);
        category.Inactive.Should().BeFalse();

        var getResponse = await adminClient.GetAsync($"categories/{category.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AddNewCategory_ShouldBeForbidden_ForReaderUser()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync("categories", new { Name = $"Category-{Guid.NewGuid()}" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddNewCategory_ShouldReturnConflict_WhenNameAlreadyExists()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var category = await SeedCategoryAsync();

        var response = await adminClient.PostAsJsonAsync("categories", new { Name = category.Name });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddNewCategory_ShouldReturnBadRequest_WhenNameMissing()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PostAsJsonAsync("categories", new { Name = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCategory_ShouldBeSuccessful()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var category = await SeedCategoryAsync();
        var newName = $"Updated-{Guid.NewGuid()}";

        var response = await adminClient.PutAsJsonAsync($"categories/{category.Id}", new { Name = newName, Inactive = true });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<ApiCategory>(JsonDefaults.Options);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be(newName);
        updated.Inactive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCategory_ShouldBeForbidden_ForReaderUser()
    {
        var category = await SeedCategoryAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PutAsJsonAsync($"categories/{category.Id}", new { Name = "New name" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PutAsJsonAsync("categories/nonexistent-category-xyz", new { Name = "New name" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_ShouldReturnConflict_WhenNameAlreadyUsedByAnotherCategory()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var categoryOne = await SeedCategoryAsync();
        var categoryTwo = await SeedCategoryAsync();

        var response = await adminClient.PutAsJsonAsync($"categories/{categoryTwo.Id}", new { Name = categoryOne.Name });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteCategory_ShouldBeSuccessful()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var category = await SeedCategoryAsync();

        var response = await adminClient.DeleteAsync($"categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await adminClient.GetAsync($"categories/{category.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ShouldBeForbidden_ForReaderUser()
    {
        var category = await SeedCategoryAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.DeleteAsync($"categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturn404_WhenCategoryDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.DeleteAsync("categories/nonexistent-category-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_ShouldReturnConflict_WhenCategoryIsAssignedToSet()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var category = await SeedCategoryAsync();

        await adminClient.PostAsJsonAsync($"sheetmusic/sets/{testSet.Id}/categories", new { CategoryIdentifier = category.Id.ToString() });

        var response = await adminClient.DeleteAsync($"categories/{category.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Category> SeedCategoryAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();

        var category = new Category { Id = Guid.NewGuid(), Name = $"Category-{Guid.NewGuid()}" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category;
    }
}
