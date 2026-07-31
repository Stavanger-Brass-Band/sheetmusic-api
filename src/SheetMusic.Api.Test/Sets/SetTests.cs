using FluentAssertions;
using Moq;
using SheetMusic.Api.Parts.ViewModels;
using SheetMusic.Api.Sets;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Sets.Models;
using SheetMusic.Api.Test.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Sets;

[Collection(Collections.Set)]
public class SetTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task GetSingleSet_AsMusikant_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSingleSet_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSet_ShouldBeForbidden_ForMusikantUser()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testBuilder = new SetDataBuilder(adminClient);
        var testSet = await testBuilder.ProvisionSingleSetAsync();
        var inputSet = testBuilder.GetRequestSet(testSet.OriginatingId);

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.PutAsJsonAsync($"sheetmusic/sets/{testSet.ArchiveNumber}", inputSet);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateSet_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testBuilder = new SetDataBuilder(adminClient);
        var testSet = await testBuilder.ProvisionSingleSetAsync();
        var inputSet = testBuilder.GetRequestSet(testSet.OriginatingId);

        inputSet.Title = $"{inputSet.Title} (updated)";
        var response = await adminClient.PutAsJsonAsync($"sheetmusic/sets/{testSet.ArchiveNumber}", inputSet);
        var body = response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSet_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testBuilder = new SetDataBuilder(adminClient);
        var testSet = await testBuilder.ProvisionSingleSetAsync();
        var inputSet = testBuilder.GetRequestSet(testSet.OriginatingId);

        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync($"sheetmusic/sets/{testSet.ArchiveNumber}", inputSet);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddNewSet_ShouldReturnConflict_WhenArchiveNumberAlreadyInUse()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var response = await adminClient.PostAsJsonAsync("sheetmusic/sets", new
        {
            Title = $"Duplicate archive number set - {Guid.NewGuid()}",
            Composer = "Test",
            ArchiveNumber = testSet.ArchiveNumber
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddNewSet_ShouldReturnBadRequest_WhenTitleIsMissing()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PostAsJsonAsync("sheetmusic/sets", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddNewSet_ShouldPersistRecordingUrl()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PostAsJsonAsync("sheetmusic/sets", new
        {
            Title = $"Set with recording - {Guid.NewGuid()}",
            RecordingUrl = "https://example.com/recording.mp3"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiSet = await response.Content.ReadFromJsonAsync<ApiSet>(JsonDefaults.Options);
        apiSet.Should().NotBeNull();
        apiSet!.RecordingUrl.Should().Be("https://example.com/recording.mp3");
    }

    [Fact]
    public async Task GetSetList_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testData = await new SetDataBuilder(adminClient)
            .WithSets(100)
            .ProvisionAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"sheetmusic/sets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);

        foreach (var testSet in testData)
        {
            items.Should().Contain(s => s.ArchiveNumber == testSet.ArchiveNumber && s.Title == testSet.Title);
        }
    }

    [Fact]
    public async Task GetSetList_WithOrderByTitleDescending_ShouldReturnSetsOrderedByTitleDescending()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        await new SetDataBuilder(adminClient)
            .WithSets(20)
            .ProvisionAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("sheetmusic/sets?$orderby=title desc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await response.Content.ReadFromJsonAsync<List<ApiSet>>(JsonDefaults.Options);

        items.Should().NotBeNull();
        items.Should().BeInDescendingOrder(s => s.Title);
    }

    [Fact]
    public async Task GetPartsForSet_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var testParts = await new PartDataBuilder(adminClient)
            .WithParts(30)
            .ProvisionAsync();

        foreach (var part in testParts)
        {
            await AddPartToSetAsync(testSet, part);
        }

        var partsResponse = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/parts");
        partsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await partsResponse.Content.ReadFromJsonAsync<ApiSet>(JsonDefaults.Options);

        foreach (var part in testParts)
        {
            items.Should().NotBeNull();
            var item = items?.Parts?.FirstOrDefault(s => s.Name == part.Name);
            item.Should().NotBeNull();
            item?.SetId.Should().Be(testSet.Id);
        }
    }

    [Fact]
    public async Task DeletePartOnSet_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(testSet, part);

        var response = await adminClient.DeleteAsync($"sheetmusic/sets/{testSet.Id}/parts/{part.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.BlobMock.Verify(b => b.DeletePartContentAsync(It.Is<PartRelatedToSet>(r => r.SetId == testSet.Id && r.PartId == part.Id)), Times.Once);
    }

    [Fact]
    public async Task DeletePartOnSet_ShouldReturn404_WhenRelationshipDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var response = await adminClient.DeleteAsync($"sheetmusic/sets/{testSet.Id}/parts/{part.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSinglePartOnSet_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        var setPart = await AddPartToSetAsync(testSet, part);
        setPart.Should().NotBeNull();
        setPart!.SetId.Should().Be(testSet.Id);
        setPart.MusicPartId.Should().Be(part.Id);
    }

    [Fact]
    public async Task GetSinglePartOnSet_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var response = await adminClient.GetAsync($"sheetmusic/sets/nonexistent-set-xyz/parts/{part.Name}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSinglePartOnSet_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var response = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/parts/nonexistent-part-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSet_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(testSet, part);

        var response = await adminClient.DeleteAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        factory.BlobMock.Verify(b => b.DeleteSetContentAsync(testSet.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteSet_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.DeleteAsync("sheetmusic/sets/nonexistent-set-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSet_ShouldReturn401_WhenUnauthenticated()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var client = factory.CreateClient();
        var response = await client.DeleteAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSinglePartFile_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(testSet, part);

        var token = await GetDownloadTokenAsync(testSet);

        var response = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Title}/parts/{part.Name}/pdf?downloadToken={token}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadPartsForSet_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var parts = await new PartDataBuilder(adminClient).WithParts(1).ProvisionAsync();

        using var memoryStream = new MemoryStream();
        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry($"{parts[0].Name}.pdf");
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(Encoding.UTF8.GetBytes("content"));
        }
        await memoryStream.FlushAsync();
        memoryStream.Position = 0;

        var response = await FileUploader.UploadOneFileAndGetResponseFromStream(memoryStream, adminClient, "sheetmusic/sets/nonexistent-set-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPartContent_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();

        var path = $"{Path.GetTempPath()}{part.Name}.pdf";
        await File.WriteAllTextAsync(path, "content");

        var response = await FileUploader.UploadOneFileAndGetResponse(path, adminClient, $"sheetmusic/sets/nonexistent-set-xyz/parts/{part.Name}/content?api-version=2.0");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPartContent_ShouldReturn404_WhenPartDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var path = $"{Path.GetTempPath()}nonexistent-part.pdf";
        await File.WriteAllTextAsync(path, "content");

        var response = await FileUploader.UploadOneFileAndGetResponse(path, adminClient, $"sheetmusic/sets/{testSet.Id}/parts/nonexistent-part-xyz/content?api-version=2.0");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPartContent_ShouldReturnConflict_WhenPartAlreadyAddedToSet()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(testSet, part);

        var path = $"{Path.GetTempPath()}{part.Name}-duplicate.pdf";
        await File.WriteAllTextAsync(path, "content");

        var response = await FileUploader.UploadOneFileAndGetResponse(path, adminClient, $"sheetmusic/sets/{testSet.Id}/parts/{part.Name}/content?api-version=2.0");
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UploadPartsForSet_ShouldCreateCorrectPartsOnSet()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var partCount = 30;
        var parts = await new PartDataBuilder(adminClient)
            .WithParts(partCount)
            .ProvisionAsync();

        await UploadPartsAsync(parts, testSet);

        var partsResponse = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/parts");
        partsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await partsResponse.Content.ReadAsStringAsync();
        Debug.Write(body);

        var set = JsonSerializer.Deserialize<ApiSet>(body, JsonDefaults.Options);
        set.Should().NotBeNull();
        set?.Parts.Should().NotBeEmpty();
        set?.Parts?.Count.Should().Be(partCount);

        foreach (var setPart in set?.Parts ?? [])
        {
            setPart.SetId.Should().Be(testSet.Id);
            parts.Should().Contain(p => p.Id == setPart.MusicPartId);
        }
    }

    [Fact]
    public async Task GetPartsAsZip_ShouldGiveCorrectContent()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var partCount = 30;
        var parts = await new PartDataBuilder(adminClient)
            .WithParts(partCount)
            .ProvisionAsync();

        await UploadPartsAsync(parts, testSet);
        factory.BlobMock.Setup(bm => bm.GetMusicPartContentStreamAsync(It.IsAny<PartRelatedToSet>())).ReturnsAsync(new MemoryStream());

        var token = await GetDownloadTokenAsync(testSet);
        var zipResponse = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/zip?downloadToken={token}");
        zipResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var memoryStream = await zipResponse.Content.ReadAsStreamAsync();
        using var zip = new ZipArchive(memoryStream);
        zip.Entries.Count.Should().Be(partCount);

        foreach (var entry in zip.Entries)
        {
            parts.Should().Contain(s => $"{s.Name}.pdf" == entry.Name);
        }
    }

    private async Task<ApiSetPart?> AddPartToSetAsync(ApiSet set, ApiPart part)
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var path = $"{Path.GetTempPath()}{part.Name}.pdf";
        await File.WriteAllTextAsync(path, "alsifaihsdfiuahwepouihagjah");
        await FileUploader.UploadOneFile(path, adminClient, $"sheetmusic/sets/{set.Id}/parts/{part.Name}/content?api-version=2.0");

        factory.BlobMock.Verify(b =>
            b.AddMusicPartContentAsync(It.Is<PartRelatedToSet>(r => r.SetId == set.Id && r.PartId == part.Id), It.IsAny<Stream>()),
            Times.Once);

        var response = await adminClient.GetAsync($"sheetmusic/sets/{set.Id}/parts/{part.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var setPart = await response.Content.ReadFromJsonAsync<ApiSetPart>(JsonDefaults.Options);

        return setPart;
    }

    private async Task<string> GetDownloadTokenAsync(ApiSet set)
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var tokenResponse = await adminClient.GetAsync($"sheetmusic/sets/{set.Id}/zip/token");
        var body = await tokenResponse.Content.ReadAsStringAsync();

        return body;
    }

    private async Task UploadPartsAsync(List<ApiPart> parts, ApiSet set)
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var content = Encoding.UTF8.GetBytes("this is just for testing purposes and is not a real PDF content string");

        using var memoryStream = new MemoryStream();
        using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var part in parts)
            {
                var entry = zip.CreateEntry($"{part.Name}.pdf");
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(content);
                await entryStream.FlushAsync();
            }
        }

        await memoryStream.FlushAsync();
        await FileUploader.UploadFromStream(memoryStream, adminClient, $"sheetmusic/sets/{set.Id}");
    }

    [Fact]
    public async Task AddNewSet_ShouldBeForbidden_WhenMusikant()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.PostAsJsonAsync("sheetmusic/sets", new { Title = "Test Set", Composer = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddNewSet_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);

        var response = await client.PostAsJsonAsync("sheetmusic/sets", new { Title = $"Noteansvarlig Set {Guid.NewGuid()}", Composer = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSet_ShouldReturn404_WhenNotFound()
    {
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync("sheetmusic/sets/nonexistent-set-xyz");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateSet_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.PutAsJsonAsync("sheetmusic/sets/nonexistent-set-xyz", new { Title = "Test", Composer = "Test" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPartsForSet_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("sheetmusic/sets/nonexistent-set-xyz/parts");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSinglePartFile_ShouldNotReturnOk_WhenNoToken()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(testSet, part);

        var response = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Title}/parts/{part.Name}/pdf");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSinglePartFile_ShouldNotReturnOk_WhenInvalidToken()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        await AddPartToSetAsync(testSet, part);

        var response = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Title}/parts/{part.Name}/pdf?downloadToken=invalid-token");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDownloadToken_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("sheetmusic/sets/nonexistent-set-xyz/zip/token");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPartsAsZip_ShouldNotReturnOk_WhenInvalidToken()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var response = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/zip?downloadToken=invalid");
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPartsAsZip_ShouldReturn404_WhenSetDoesNotExist()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("sheetmusic/sets/nonexistent-set-xyz/zip?downloadToken=irrelevant");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSetsThatHasPartsButNoFiles_ShouldBeSuccessful()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var response = await adminClient.GetAsync("sheetmusic/sets/withoutFiles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteSet_ShouldBeForbidden_WhenMusikant()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);
        var response = await client.DeleteAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteSet_ShouldBeSuccessful_WhenNoteansvarlig()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Noteansvarlig);
        var response = await client.DeleteAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
