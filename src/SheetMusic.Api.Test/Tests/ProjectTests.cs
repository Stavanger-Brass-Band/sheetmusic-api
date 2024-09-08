using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Utility;
using System;
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
}
