using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Models;
using SheetMusic.Api.Test.Utility;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests;

[Collection(Collections.Set)]
public class SetTests(SheetMusicWebAppFactory factory) : IClassFixture<SheetMusicWebAppFactory>
{
    [Fact]
    public async Task GetSingleSet_AsReader_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);

        var testSet = await new SetDataBuilder(adminClient).ProvisionSingleSetAsync();
        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"sheetmusic/sets/{testSet.ArchiveNumber}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateSet_ShouldBeForbidden_ForReaderUser()
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
    public async Task GetSetList_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testData = await new SetDataBuilder(adminClient)
            .WithSets(100)
            .ProvisionAsync();

        var client = factory.CreateClientWithTestToken(TestUser.Testesen);

        var response = await client.GetAsync($"sheetmusic/sets");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var items = JsonConvert.DeserializeObject<List<ApiSet>>(body);

        foreach (var testSet in testData)
        {
            items.Should().Contain(s => s.ArchiveNumber == testSet.ArchiveNumber && s.Title == testSet.Title);
        }
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

        var body = await partsResponse.Content.ReadAsStringAsync();
        var items = JsonConvert.DeserializeObject<ApiSet>(body);

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
    public async Task GetSinglePartOnSet_ShouldBeSuccessfull()
    {
        var adminClient = factory.CreateClientWithTestToken(TestUser.Administrator);
        var testSet = await new SetDataBuilder(adminClient)
            .ProvisionSingleSetAsync();

        var part = await new PartDataBuilder(adminClient).ProvisionSinglePartAsync();
        var setPart = await AddPartToSetAsync(testSet, part);

        setPart.SetId.Should().Be(testSet.Id);
        setPart.MusicPartId.Should().Be(part.Id);

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

        var set = JsonConvert.DeserializeObject<ApiSet>(body);
        set.Should().NotBeNull();
        set.Parts.Should().NotBeEmpty();
        set?.Parts?.Count.Should().Be(partCount);

        foreach (var setPart in set?.Parts ?? new List<ApiSetPart>())
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

        using (var memoryStream = await zipResponse.Content.ReadAsStreamAsync())
        {
            using (var zip = new ZipArchive(memoryStream))
            {
                zip.Entries.Count.Should().Be(partCount);

                foreach (var entry in zip.Entries)
                {
                    parts.Should().Contain(s => $"{s.Name}.pdf" == entry.Name);
                }
            }
        }
    }

    private async Task<ApiSetPart> AddPartToSetAsync(ApiSet set, ApiPart part)
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

        var body = await response.Content.ReadAsStringAsync();
        var setPart = JsonConvert.DeserializeObject<ApiSetPart>(body);

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

        using (var memoryStream = new MemoryStream())
        {
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
    }
}
