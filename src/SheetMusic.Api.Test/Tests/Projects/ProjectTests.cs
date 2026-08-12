using FluentAssertions;
using SheetMusic.Api.Projects.ViewModels;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Utility;
using ApiSet = SheetMusic.Api.Sets.ViewModels.ApiSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public async Task UpdateProject_ShouldBeForbidden_WhenUserIsMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = "New project - Musikant",
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
    public async Task GetCatalogResources_ShouldRespectActiveProjectScope_ForMusikantArkivleserAndProsjektleder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var activeProject = new
        {
            Name = $"Active project - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            EndDate = DateTimeOffset.UtcNow.AddDays(1)
        };
        var inactiveProject = new
        {
            Name = $"Inactive project - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddDays(-3),
            EndDate = DateTimeOffset.UtcNow.AddDays(-2)
        };
        await adminClient.PostAsJsonAsync("projects", activeProject);
        await adminClient.PostAsJsonAsync("projects", inactiveProject);

        var activeSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var inactiveSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        await adminClient.PostAsJsonAsync($"projects/{activeProject.Name}/sets", new { SetIdentifiers = new[] { activeSet.Id.ToString() } });
        await adminClient.PostAsJsonAsync($"projects/{inactiveProject.Name}/sets", new { SetIdentifiers = new[] { inactiveSet.Id.ToString() } });

        var musikantClient = factory.CreateClientWithTestToken(TestUser.Musikant);
        var musikantProjects = await musikantClient.GetFromJsonAsync<List<ApiProject>>("projects", JsonDefaults.Options);
        musikantProjects!.Should().Contain(project => project.Name == activeProject.Name);
        musikantProjects.Should().NotContain(project => project.Name == inactiveProject.Name);
        var musikantSets = await musikantClient.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        musikantSets!.Should().Contain(set => set.Id == activeSet.Id);
        musikantSets.Should().NotContain(set => set.Id == inactiveSet.Id);
        (await musikantClient.GetAsync($"sheetmusic/sets/{inactiveSet.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await musikantClient.GetAsync($"sheetmusic/sets/{inactiveSet.Id}/zip/token")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var activeTokenResponse = await musikantClient.GetAsync($"sheetmusic/sets/{activeSet.Id}/zip/token");
        activeTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var activeToken = await activeTokenResponse.Content.ReadAsStringAsync();
        var anonymousClient = factory.CreateClient();
        (await anonymousClient.GetAsync($"sheetmusic/sets/{activeSet.Id}/zip?downloadToken={activeToken.Trim('"')}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        var concurrentToken = (await (await musikantClient.GetAsync($"sheetmusic/sets/{activeSet.Id}/zip/token")).Content.ReadAsStringAsync()).Trim('"');
        var concurrentDownloads = await Task.WhenAll(
            anonymousClient.GetAsync($"sheetmusic/sets/{activeSet.Id}/zip?downloadToken={concurrentToken}"),
            anonymousClient.GetAsync($"sheetmusic/sets/{activeSet.Id}/zip?downloadToken={concurrentToken}"));
        concurrentDownloads.Should().ContainSingle(response => response.StatusCode == HttpStatusCode.OK);

        var arkivleserClient = factory.CreateClientWithTestToken(TestUser.Arkivleser);
        var arkivleserProjects = await arkivleserClient.GetFromJsonAsync<List<ApiProject>>("projects", JsonDefaults.Options);
        arkivleserProjects!.Should().Contain(project => project.Name == inactiveProject.Name);
        var arkivleserSets = await arkivleserClient.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        arkivleserSets!.Should().Contain(set => set.Id == inactiveSet.Id);
        (await arkivleserClient.GetAsync($"sheetmusic/sets/{inactiveSet.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await arkivleserClient.GetAsync($"sheetmusic/sets/{inactiveSet.Id}/zip/token")).StatusCode.Should().Be(HttpStatusCode.OK);

        var administratorSets = await adminClient.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        administratorSets!.Should().Contain(set => set.Id == inactiveSet.Id);
        var dualRoleClient = factory.CreateClientWithTestToken(TestUser.Testesen);
        (await dualRoleClient.GetAsync($"sheetmusic/sets/{inactiveSet.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var noCatalogAccessClient = factory.CreateClientWithTestToken(TestUser.Prosjektleder);
        var prosjektlederProjects = await noCatalogAccessClient.GetFromJsonAsync<List<ApiProject>>("projects", JsonDefaults.Options);
        prosjektlederProjects!.Should().Contain(project => project.Name == activeProject.Name);
        prosjektlederProjects.Should().Contain(project => project.Name == inactiveProject.Name);
        (await noCatalogAccessClient.GetAsync($"sheetmusic/sets/{activeSet.Id}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
    public async Task CreateProject_ShouldBeForbidden_WhenMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync("projects", new { Name = "Should fail", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1) });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateProject_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.PostAsJsonAsync("projects", new { Name = $"Noteansvarlig project - {Guid.NewGuid():N}", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1) });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
    public async Task AssignSetToProject_ShouldBeForbidden_WhenMusikant()
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
    public async Task DeleteProject_ShouldBeForbidden_WhenMusikant()
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
    public async Task DeleteProject_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Delete project noteansvarlig test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);
        var response = await client.DeleteAsync($"projects/{project.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
    public async Task UnassignSetFromProject_ShouldBeForbidden_WhenMusikant()
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

    #region Prosjektleder

    [Fact]
    public async Task CreateProject_ShouldBeSuccessful_WhenProsjektleder()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);

        var response = await client.PostAsJsonAsync("projects", new { Name = $"Prosjektleder project - {Guid.NewGuid():N}", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1) });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProject_ShouldBeSuccessful_WhenProsjektleder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Update project prosjektleder test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);
        var response = await client.PutAsJsonAsync($"projects/{project.Name}",
            new
            {
                project.Name,
                Comments = "Updated by prosjektleder",
                project.StartDate,
                project.EndDate
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedProject = await response.Content.ReadFromJsonAsync<ApiProject>(JsonDefaults.Options);
        updatedProject.Should().NotBeNull();
        updatedProject!.Comments.Should().Be("Updated by prosjektleder");
    }

    [Fact]
    public async Task DeleteProject_ShouldBeSuccessful_WhenProsjektleder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Delete project prosjektleder test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);
        var response = await client.DeleteAsync($"projects/{project.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await adminClient.GetAsync($"projects/{project.Name}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignSetToProject_ShouldBeSuccessful_WhenProsjektleder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Assign set prosjektleder test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);
        var response = await client.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var setsResponse = await adminClient.GetAsync($"projects/{project.Name}/sets");
        var sets = await setsResponse.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets.Should().Contain(s => s.Id == testSet.Id);
    }

    [Fact]
    public async Task UnassignSetFromProject_ShouldBeSuccessful_WhenProsjektleder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var project = new
        {
            Name = $"Unassign set prosjektleder test - {Guid.NewGuid():N}",
            StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
            EndDate = DateTimeOffset.UtcNow.AddMonths(1)
        };
        await adminClient.PostAsJsonAsync("projects", project);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        await adminClient.PostAsJsonAsync($"projects/{project.Name}/sets",
            new { SetIdentifiers = new List<string> { testSet.Id.ToString() } });

        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);
        var response = await client.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, $"projects/{project.Name}/sets/")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { SetIdentifiers = new List<string> { testSet.Id.ToString() } })
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setsResponse = await adminClient.GetAsync($"projects/{project.Name}/sets");
        var sets = await setsResponse.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);
        sets.Should().NotContain(s => s.Id == testSet.Id);
    }

    [Fact]
    public async Task UpdateSet_ShouldBeForbidden_WhenProsjektleder()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);
        var response = await client.PutAsJsonAsync($"sheetmusic/sets/{testSet.Id}",
            new { Title = "Should be forbidden", Composer = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region OData $orderby, $skip and $top

    private static async Task SeedProjectsAsync(HttpClient client, params string[] names)
    {
        foreach (var name in names)
        {
            await client.PostAsJsonAsync("projects", new
            {
                Name = name,
                StartDate = DateTimeOffset.UtcNow.AddMonths(-1),
                EndDate = DateTimeOffset.UtcNow.AddMonths(1)
            });
        }
    }

    [Fact]
    public async Task GetProjects_ShouldRespectOrderBy_WhenOrderByAscendingProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Zulu project", "Alfa project", "Mike project");

        var response = await client.GetAsync("projects?$orderby=name asc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects!.Select(p => p.Name).Should().Equal("Alfa project", "Mike project", "Zulu project");
    }

    [Fact]
    public async Task GetProjects_ShouldRespectOrderBy_WhenOrderByDescendingProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Zulu project", "Alfa project", "Mike project");

        var response = await client.GetAsync("projects?$orderby=name desc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects!.Select(p => p.Name).Should().Equal("Zulu project", "Mike project", "Alfa project");
    }

    [Fact]
    public async Task GetProjects_ShouldRespectTop_WhenTopProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Alfa project", "Bravo project", "Charlie project");

        var response = await client.GetAsync("projects?$orderby=name asc&$top=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects!.Select(p => p.Name).Should().Equal("Alfa project", "Bravo project");
    }

    [Fact]
    public async Task GetProjects_ShouldRespectSkipAndTop_WhenBothProvided()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Alfa project", "Bravo project", "Charlie project", "Delta project");

        var response = await client.GetAsync("projects?$orderby=name asc&$skip=1&$top=2");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects!.Select(p => p.Name).Should().Equal("Bravo project", "Charlie project");
    }

    #endregion

    #region OData $search

    [Fact]
    public async Task GetProjects_WithSearchOnName_ShouldReturnMatchingProjects()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Oppstart 2026", "Konsert 2026", "Sommerfest");

        var response = await client.GetAsync($"projects?$search={Uri.EscapeDataString("Oppstart")}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects.Should().ContainSingle();
        projects![0].Name.Should().Be("Oppstart 2026");
    }

    [Fact]
    public async Task GetProjects_WithSearchOnName_ShouldReturnEmptyArray_WhenNoMatches()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Oppstart 2026", "Konsert 2026");

        var response = await client.GetAsync($"projects?$search={Uri.EscapeDataString("no-project-will-match-this")}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjects_WithSearchAndFilter_ShouldApplyBoth()
    {
        using var isolatedFactory = new SheetMusicWebAppFactory();
        var client = isolatedFactory.CreateClientWithTestToken(TestUser.Administrator);
        await SeedProjectsAsync(client, "Oppstart 2026", "Oppstart 2027", "Konsert 2026");

        var response = await client.GetAsync($"projects?$search={Uri.EscapeDataString("Oppstart")}&$filter={Uri.EscapeDataString("name eq 'Oppstart 2027'")}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<List<ApiProject>>(JsonDefaults.Options);
        projects.Should().ContainSingle();
        projects![0].Name.Should().Be("Oppstart 2027");
    }

    #endregion
}
