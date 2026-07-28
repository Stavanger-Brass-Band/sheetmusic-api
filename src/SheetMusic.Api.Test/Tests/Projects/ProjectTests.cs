using FluentAssertions;
using SheetMusic.Api.Projects.ViewModels;
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

namespace SheetMusic.Api.Test.Tests.Projects;

[Collection(Collections.Project)]
public class ProjectTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task UpdateProject_ShouldUpdateProjectSuccessfully_WhenUserIsAdmin()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = "New project - Admin",
            Comments = "This is a long comment",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };

        await client.PostAsJsonAsync($"projects", project);

        var response = await client.PutAsJsonAsync($"projects/{project.Name}",
            new
            {
                project.Name,
                Comments = "This is a long comment",
                project.StartDate,
                project.EndDate
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProject_ShouldPersistComments_WhenProvided()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Update comments test - {Guid.NewGuid():N}",
            Comments = "Original comment",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };

        await client.PostAsJsonAsync("projects", project);

        var response = await client.PutAsJsonAsync($"projects/{project.Name}",
            new
            {
                project.Name,
                Comments = "Updated comment",
                project.StartDate,
                project.EndDate
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedProject = await response.Content.ReadFromJsonAsync<ApiProject>(JsonDefaults.Options);
        updatedProject.Should().NotBeNull();
        updatedProject!.Comments.Should().Be("Updated comment");
    }

    [Fact]
    public async Task UpdateProject_ShouldBeForbidden_WhenUserIsReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = "New project - Reader",
            Comments = "This is a long comment",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };

        await client.PostAsJsonAsync($"projects", project);

        client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PutAsJsonAsync($"projects/{project.Name}",
        new
        {
            project.Name,
            Comments = "This is a long comment",
            project.StartDate,
            project.EndDate
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PutAsJsonAsync($"projects/{Guid.NewGuid()}",
            new
            {
                Name = "Does not exist",
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(1)
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjects_ShouldBeSuccessful_WhenAuthenticated()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("projects");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProjects_ShouldReturn401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("projects");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProject_ShouldReturnProject_WhenExists()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Get project test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };

        await adminClient.PostAsJsonAsync("projects", project);

        var response = await adminClient.GetAsync($"projects/{project.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"projects/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProject_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync("projects", new { Name = "Should fail", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1) });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProject_ShouldReturn401_WhenUnauthenticated()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("projects", new { Name = "Should fail", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1) });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_ShouldBeSuccessful_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Admin project - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };

        var response = await adminClient.PostAsJsonAsync("projects", project);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProject_ShouldPersistComments_WhenProvided()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Project with comments - {Guid.NewGuid():N}",
            Comments = "This is a project comment",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };

        var response = await adminClient.PostAsJsonAsync("projects", project);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var createdProject = await response.Content.ReadFromJsonAsync<ApiProject>(JsonDefaults.Options);
        createdProject.Should().NotBeNull();
        createdProject!.Comments.Should().Be(project.Comments);

        var getResponse = await adminClient.GetAsync($"projects/{project.Name}");
        var fetchedProject = await getResponse.Content.ReadFromJsonAsync<ApiProject>(JsonDefaults.Options);
        fetchedProject.Should().NotBeNull();
        fetchedProject!.Comments.Should().Be(project.Comments);
    }

    [Fact]
    public async Task AssignSetToProject_ShouldBeSuccessful_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Assign set test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var response = await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AssignSetToProject_ShouldBeForbidden_WhenReader()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Assign set forbidden - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { Guid.NewGuid().ToString() } });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignSetToProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var response = await adminClient.PostAsJsonAsync($"projects/{Guid.NewGuid()}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSetsForProject_ShouldReturnSets_WhenAssigned()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Get sets test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });

        var response = await adminClient.GetAsync($"projects/{project.Name}/sets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sets = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets.Should().NotBeNull();
        sets!.Should().Contain(s => s.Id == testSet.Id);
    }

    [Fact]
    public async Task GetSetsForProject_ShouldReturnEmptyList_WhenNoSetsAssigned()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Empty sets test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var response = await adminClient.GetAsync($"projects/{project.Name}/sets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var sets = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignSetToProject_ShouldReorderExistingSets_WhenCalledAgainWithNewOrder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Reorder sets test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSets = await new SetDataBuilder(adminClient).WithSets(2).ProvisionAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = testSets.Select(s => s.Id.ToString()) });

        var reversedOrder = testSets.Select(s => s.Id.ToString()).Reverse().ToList();

        var response = await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = reversedOrder });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var orderedSets = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        orderedSets.Should().NotBeNull();
        orderedSets!.Select(s => s.Id.ToString()).Should().ContainInOrder(reversedOrder);

        var getResponse = await adminClient.GetAsync($"projects/{project.Name}/sets");
        var sets = await getResponse.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets!.Select(s => s.Id.ToString()).Should().ContainInOrder(reversedOrder);
    }

    [Fact]
    public async Task AssignSetToProject_ShouldAppendNewSet_WithoutDisturbingExistingOrder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Append set test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSets = await new SetDataBuilder(adminClient).WithSets(2).ProvisionAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = testSets.Select(s => s.Id.ToString()) });

        var additionalSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { additionalSet.Id.ToString() } });

        var expectedOrder = testSets.Select(s => s.Id.ToString()).Append(additionalSet.Id.ToString()).ToList();

        var getResponse = await adminClient.GetAsync($"projects/{project.Name}/sets");
        var sets = await getResponse.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets!.Select(s => s.Id.ToString()).Should().ContainInOrder(expectedOrder);
    }

    [Fact]
    public async Task DeleteProject_ShouldRemoveProject_WhenExists()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Delete project test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var response = await adminClient.DeleteAsync($"projects/{project.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await adminClient.GetAsync($"projects/{project.Name}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.DeleteAsync($"projects/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProject_ShouldBeForbidden_WhenReader()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Delete project forbidden test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"projects/{project.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnassignSetFromProject_ShouldRemoveSet_WhenAdmin()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Unassign set test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });

        var response = await adminClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, $"projects/{project.Name}/sets/")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { SetIdentifiers = new List<string> { testSet.Id.ToString() } })
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setsResponse = await adminClient.GetAsync($"projects/{project.Name}/sets");
        var sets = await setsResponse.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets.Should().NotContain(s => s.Id == testSet.Id);
    }

    [Fact]
    public async Task UnassignSetFromProject_ShouldBeForbidden_WhenReader()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Unassign set forbidden test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, $"projects/{project.Name}/sets/")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { SetIdentifiers = new List<string> { testSet.Id.ToString() } })
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnassignSetFromProject_ShouldReturn404_WhenProjectDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, $"projects/{Guid.NewGuid()}/sets/")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { SetIdentifiers = new List<string> { Guid.NewGuid().ToString() } })
        });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
