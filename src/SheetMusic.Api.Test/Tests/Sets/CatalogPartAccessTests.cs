using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.Database;
using SheetMusic.Api.Database.Entities;
using SheetMusic.Api.Parts;
using SheetMusic.Api.Sets.ViewModels;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests.Sets;

public class CatalogPartAccessTests
{
    [Fact]
    public async Task GetCatalogue_ShouldFilterSetsAndParts_WhenUserIsMusikantOnly()
    {
        using var factory = new SheetMusicWebAppFactory();
        var corpus = await SeedCatalogueAsync(factory);
        var client = factory.CreateClientWithTestToken(TestUser.Musikant);

        var pagedSets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets?$top=2", JsonDefaults.Options);
        pagedSets!.Select(set => set.Id).Should().Equal(corpus.GroupSetId, corpus.SecondGroupSetId);
        var skippedSets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets?$skip=1&$top=1", JsonDefaults.Options);
        skippedSets!.Select(set => set.Id).Should().Equal(corpus.SecondGroupSetId);

        var expandedSets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets?$expand=parts", JsonDefaults.Options);
        expandedSets!.Select(set => set.Id).Should().BeEquivalentTo([
            corpus.GroupSetId,
            corpus.SecondGroupSetId,
            corpus.DirectSetId,
            corpus.PartiturSetId
        ]);
        var expandedGroupSet = expandedSets!.Single(set => set.Id == corpus.GroupSetId);
        expandedGroupSet.Parts!.Select(part => part.MusicPartId).Should().BeEquivalentTo([
            corpus.GroupPartId,
            corpus.SecondGroupPartId,
            corpus.DirectPartId
        ]);

        var setWithParts = await client.GetFromJsonAsync<ApiSet>($"sheetmusic/sets/{corpus.GroupSetId}/parts", JsonDefaults.Options);
        setWithParts!.Parts!.Select(part => part.MusicPartId).Should().BeEquivalentTo([
            corpus.GroupPartId,
            corpus.SecondGroupPartId,
            corpus.DirectPartId
        ]);

        (await client.GetAsync($"sheetmusic/sets/{corpus.HiddenSetId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"sheetmusic/sets/{corpus.GroupSetId}/parts/{corpus.OutOfGroupPartId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"sheetmusic/sets/{corpus.HiddenSetId}/categories")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("sheetmusic/agent/chat", new { SetName = corpus.HiddenSetTitle, Question = "Composer?" })).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCatalogue_ShouldReflectPartChangesOnNextRequest_WhenUserIsMusikantOnly()
    {
        using var factory = new SheetMusicWebAppFactory();
        var corpus = await SeedCatalogueAsync(factory);
        var client = factory.CreateClientWithTestToken(TestUser.Musikant);

        var initialSets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        initialSets!.Select(set => set.Id).Should().Contain(corpus.DirectSetId).And.NotContain(corpus.HiddenSetId);

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            var assignedGroupPart = await db.MusicParts.SingleAsync(part => part.Id == corpus.AssignedGroupPartId);
            assignedGroupPart.InstrumentGroup = InstrumentGroup.Tuba;
            var directPart = await db.MusicParts.SingleAsync(part => part.Id == corpus.DirectPartId);
            directPart.Indexable = false;
            await db.SaveChangesAsync();
        }

        var changedSets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        changedSets!.Select(set => set.Id).Should().Contain(corpus.HiddenSetId).And.NotContain(corpus.DirectSetId);

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            var assignments = await db.Set<MusicianMusicPart>()
                .Where(assignment => assignment.Musician.ApplicationUserId == TestUser.Musikant.Identifier)
                .ToListAsync();
            db.RemoveRange(assignments);
            await db.SaveChangesAsync();
        }

        var setsWithoutAssignments = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        setsWithoutAssignments!.Select(set => set.Id).Should().Equal(corpus.PartiturSetId);
        (await client.GetAsync($"sheetmusic/sets/{corpus.HiddenSetId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCatalogue_ShouldReturnNoAccess_WhenLinkedMusicianIsMissing()
    {
        using var factory = new SheetMusicWebAppFactory();
        var corpus = await SeedCatalogueAsync(factory);
        var client = factory.CreateClientWithTestToken(TestUser.Musikant);

        var setsWithMusician = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        setsWithMusician!.Select(set => set.Id).Should().Contain(corpus.GroupSetId);

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            var musician = await db.Musicians.SingleAsync(item => item.ApplicationUserId == TestUser.Musikant.Identifier);
            db.Musicians.Remove(musician);
            await db.SaveChangesAsync();
            (await db.Musicians.AnyAsync(item => item.ApplicationUserId == TestUser.Musikant.Identifier)).Should().BeFalse();
        }

        var setsWithoutMusician = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets", JsonDefaults.Options);
        setsWithoutMusician!.Select(set => set.Id).Should().Equal(corpus.PartiturSetId);
        (await client.GetAsync($"sheetmusic/sets/{corpus.GroupSetId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DownloadToken_ShouldRestrictIndividualPdfButAllowCompleteZip_WhenMusikantQualifiesForSet()
    {
        using var factory = new SheetMusicWebAppFactory();
        var corpus = await SeedCatalogueAsync(factory);
        var client = factory.CreateClientWithTestToken(TestUser.Musikant);

        var tokenResponse = await client.GetAsync($"sheetmusic/sets/{corpus.GroupSetId}/zip/token");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await tokenResponse.Content.ReadAsStringAsync()).Trim('"');
        var anonymousClient = factory.CreateClient();

        (await anonymousClient.GetAsync($"sheetmusic/sets/{corpus.GroupSetId}/parts/{corpus.OutOfGroupPartId}/pdf?downloadToken={token}"))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var zipResponse = await anonymousClient.GetAsync($"sheetmusic/sets/{corpus.GroupSetId}/zip?downloadToken={token}");
        zipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var zipContent = new MemoryStream(await zipResponse.Content.ReadAsByteArrayAsync());
        using var archive = new ZipArchive(zipContent, ZipArchiveMode.Read);
        archive.Entries.Select(entry => entry.Name).Should().BeEquivalentTo(
            corpus.GroupSetPartNames.Select(name => $"{name}.pdf"));
    }

    [Fact]
    public async Task GetCatalogue_ShouldNotFilter_WhenUserAlsoHasFullAccessRole()
    {
        using var factory = new SheetMusicWebAppFactory();
        var corpus = await SeedCatalogueAsync(factory);
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var sets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets?$expand=parts", JsonDefaults.Options);

        sets!.Select(set => set.Id).Should().Contain([corpus.HiddenSetId, corpus.InactiveSetId]);
        sets!.Single(set => set.Id == corpus.GroupSetId).Parts!.Select(part => part.MusicPartId)
            .Should().Contain(corpus.OutOfGroupPartId);
    }

    [Fact]
    public async Task GetCatalogue_ShouldExposeOnlyPartiturToEveryAuthenticatedUser_WhenUserIsNotMusikant()
    {
        using var factory = new SheetMusicWebAppFactory();
        var corpus = await SeedCatalogueAsync(factory);
        var client = factory.CreateClientWithTestToken(TestUser.Prosjektleder);

        var sets = await client.GetFromJsonAsync<List<ApiSet>>("sheetmusic/sets?$expand=parts", JsonDefaults.Options);
        var set = sets!.Single(item => item.Id == corpus.PartiturSetId);
        set.Parts!.Select(part => part.MusicPartId).Should().Equal(corpus.PartiturPartId);
        var alwaysDisplaySet = sets!.Single(item => item.Id == corpus.AlwaysDisplaySetId);
        alwaysDisplaySet.Parts!.Select(part => part.MusicPartId).Should().Equal(corpus.AlwaysDisplayPartId);

        (await client.GetAsync($"sheetmusic/sets/{corpus.PartiturSetId}/parts/{corpus.PartiturPartId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"sheetmusic/sets/{corpus.AlwaysDisplaySetId}/parts/{corpus.AlwaysDisplayPartId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"sheetmusic/sets/{corpus.PartiturSetId}/parts/{corpus.OutOfGroupPartId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var alwaysDisplayTokenResponse = await client.GetAsync($"sheetmusic/sets/{corpus.AlwaysDisplaySetId}/zip/token");
        alwaysDisplayTokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var alwaysDisplayToken = (await alwaysDisplayTokenResponse.Content.ReadAsStringAsync()).Trim('"');
        var anonymousClient = factory.CreateClient();
        (await anonymousClient.GetAsync($"sheetmusic/sets/{corpus.AlwaysDisplaySetId}/parts/{corpus.AlwaysDisplayPartId}/pdf?downloadToken={alwaysDisplayToken}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var tokenResponse = await client.GetAsync($"sheetmusic/sets/{corpus.PartiturSetId}/zip/token");
        tokenResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await tokenResponse.Content.ReadAsStringAsync()).Trim('"');
        var zipResponse = await anonymousClient.GetAsync($"sheetmusic/sets/{corpus.PartiturSetId}/zip?downloadToken={token}");
        zipResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var zipContent = new MemoryStream(await zipResponse.Content.ReadAsByteArrayAsync());
        using var archive = new ZipArchive(zipContent, ZipArchiveMode.Read);
        archive.Entries.Select(entry => entry.Name).Should().BeEquivalentTo("Partitur.pdf", $"{corpus.OutOfGroupPartName}.pdf");

        using (var scope = factory.TestServices.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
            db.RemoveRange(db.Set<MusicianMusicPart>().Where(assignment => assignment.Musician.ApplicationUserId == TestUser.Musikant.Identifier));
            await db.SaveChangesAsync();
        }

        var musikantClient = factory.CreateClientWithTestToken(TestUser.Musikant);
        var musikantParts = await musikantClient.GetFromJsonAsync<ApiSet>($"sheetmusic/sets/{corpus.PartiturSetId}/parts", JsonDefaults.Options);
        musikantParts!.Parts!.Select(part => part.MusicPartId).Should().Equal(corpus.PartiturPartId);
    }

    private static async Task<CatalogueCorpus> SeedCatalogueAsync(SheetMusicWebAppFactory factory)
    {
        _ = factory.CreateClient();

        var assignedGroupPart = Part("Assigned cornet", InstrumentGroup.Kornett, indexable: false);
        var groupPart = Part("Other cornet", InstrumentGroup.Kornett, indexable: false);
        var assignedSecondGroupPart = Part("Assigned horn", InstrumentGroup.HornOgFlygelhorn, indexable: false);
        var secondGroupPart = Part("Other horn", InstrumentGroup.HornOgFlygelhorn, indexable: false);
        var outOfGroupPart = Part("Tuba", InstrumentGroup.Tuba, indexable: true);
        var directPart = Part("Direct null group", null, indexable: true);
        var nonIndexableDirectPart = Part("Non-indexable direct", null, indexable: false);
        var unassignedNullPart = Part("Unassigned null group", null, indexable: true);
        var partitur = new MusicPart { Id = Guid.NewGuid(), Name = "Partitur", SortOrder = 1, Indexable = false };
        var alwaysDisplayPart = new MusicPart { Id = Guid.NewGuid(), Name = "Alle stemmer", SortOrder = 1, Indexable = false, AlwaysDisplay = true };

        var groupSet = Set(1001, "Visible grouped set");
        var hiddenSet = Set(1002, "Hidden set");
        var secondGroupSet = Set(1003, "Second visible grouped set");
        var inactiveSet = Set(1004, "Inactive grouped set");
        var directSet = Set(1005, "Visible direct set");
        var partiturSet = Set(1006, "Always visible Partitur set");
        var alwaysDisplaySet = Set(1007, "Always visible Alle stemmer set");
        var activeProject = Project("Active", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));
        var inactiveProject = Project("Inactive", DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-2));

        using var scope = factory.TestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SheetMusicContext>();
        await db.MusicParts.AddRangeAsync(
            assignedGroupPart,
            groupPart,
            assignedSecondGroupPart,
            secondGroupPart,
            outOfGroupPart,
            directPart,
            nonIndexableDirectPart,
            unassignedNullPart,
            partitur,
            alwaysDisplayPart);
        await db.SheetMusicSets.AddRangeAsync(groupSet, hiddenSet, secondGroupSet, inactiveSet, directSet, partiturSet, alwaysDisplaySet);
        await db.Projects.AddRangeAsync(activeProject, inactiveProject);
        await db.SheetMusicParts.AddRangeAsync(
            SetPart(groupSet, groupPart),
            SetPart(groupSet, secondGroupPart),
            SetPart(groupSet, outOfGroupPart),
            SetPart(groupSet, directPart),
            SetPart(groupSet, nonIndexableDirectPart),
            SetPart(groupSet, unassignedNullPart),
            SetPart(hiddenSet, outOfGroupPart),
            SetPart(secondGroupSet, groupPart),
            SetPart(inactiveSet, groupPart),
            SetPart(directSet, directPart),
            SetPart(partiturSet, partitur),
            SetPart(partiturSet, outOfGroupPart),
            SetPart(alwaysDisplaySet, alwaysDisplayPart));
        await db.ProjectSheetMusicSets.AddRangeAsync(
            Connection(activeProject, groupSet),
            Connection(activeProject, hiddenSet),
            Connection(activeProject, secondGroupSet),
            Connection(inactiveProject, inactiveSet),
            Connection(activeProject, directSet));
        await db.Set<MusicianMusicPart>().AddRangeAsync(
            Assignment(TestUser.Musikant.Identifier, assignedGroupPart.Id),
            Assignment(TestUser.Musikant.Identifier, assignedSecondGroupPart.Id),
            Assignment(TestUser.Musikant.Identifier, directPart.Id),
            Assignment(TestUser.Musikant.Identifier, nonIndexableDirectPart.Id));
        await db.SaveChangesAsync();

        return new CatalogueCorpus(
            groupSet.Id,
            hiddenSet.Id,
            hiddenSet.Title,
            secondGroupSet.Id,
            inactiveSet.Id,
            directSet.Id,
            assignedGroupPart.Id,
            groupPart.Id,
            secondGroupPart.Id,
            outOfGroupPart.Id,
            directPart.Id,
            partiturSet.Id,
            partitur.Id,
            alwaysDisplaySet.Id,
            alwaysDisplayPart.Id,
            outOfGroupPart.Name,
            [groupPart.Name, secondGroupPart.Name, outOfGroupPart.Name, directPart.Name, nonIndexableDirectPart.Name, unassignedNullPart.Name]);
    }

    private static MusicPart Part(string name, InstrumentGroup? group, bool indexable) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{name} {Guid.NewGuid():N}",
        InstrumentGroup = group,
        Indexable = indexable
    };

    private static SheetMusicSet Set(int archiveNumber, string title) => new(archiveNumber, $"{title} {Guid.NewGuid():N}");

    private static Project Project(string name, DateTime startDate, DateTime endDate) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"{name} {Guid.NewGuid():N}",
        StartDate = startDate,
        EndDate = endDate
    };

    private static SheetMusicPart SetPart(SheetMusicSet set, MusicPart part) => new()
    {
        Id = Guid.NewGuid(),
        SetId = set.Id,
        MusicPartId = part.Id
    };

    private static ProjectSheetMusicSet Connection(Project project, SheetMusicSet set) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = project.Id,
        SheetMusicSetId = set.Id
    };

    private static MusicianMusicPart Assignment(Guid musicianId, Guid partId) => new()
    {
        Id = Guid.NewGuid(),
        MusicianId = musicianId,
        MusicPartId = partId
    };

    private sealed record CatalogueCorpus(
        Guid GroupSetId,
        Guid HiddenSetId,
        string HiddenSetTitle,
        Guid SecondGroupSetId,
        Guid InactiveSetId,
        Guid DirectSetId,
        Guid AssignedGroupPartId,
        Guid GroupPartId,
        Guid SecondGroupPartId,
        Guid OutOfGroupPartId,
        Guid DirectPartId,
        Guid PartiturSetId,
        Guid PartiturPartId,
        Guid AlwaysDisplaySetId,
        Guid AlwaysDisplayPartId,
        string OutOfGroupPartName,
        IReadOnlyList<string> GroupSetPartNames);
}
