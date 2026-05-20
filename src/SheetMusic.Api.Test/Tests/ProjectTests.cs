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
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests;

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
    public async Task GetProjects_ShouldBeSuccessful_WhenAuthenticated()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("projects");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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
    public async Task CreateProject_ShouldBeForbidden_WhenReader()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync("projects", new { Name = "Should fail", StartDate = DateTimeOffset.UtcNow, EndDate = DateTimeOffset.UtcNow.AddDays(1) });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

        var body = await response.Content.ReadAsStringAsync();
        var sets = JsonConvert.DeserializeObject<List<ApiSet>>(body);
        sets.Should().NotBeNull();
        sets!.Should().Contain(s => s.Id == testSet.Id);
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
        var body = await setsResponse.Content.ReadAsStringAsync();
        var sets = JsonConvert.DeserializeObject<List<ApiSet>>(body);
        sets.Should().NotContain(s => s.Id == testSet.Id);
    }
}
