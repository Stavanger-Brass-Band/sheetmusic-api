using FluentAssertions;
using Newtonsoft.Json;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Models;
using SheetMusic.Api.Test.Utility;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests
{
    [Collection(Collections.Set)]
    public class SetTests : IClassFixture<SheetMusicWebAppFactory>
    {
        private readonly SheetMusicWebAppFactory factory;

        public SetTests(SheetMusicWebAppFactory factory)
        {
            this.factory = factory;
        }

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
                var path = $"{Path.GetTempPath()}{part.Name}.pdf";
                await File.WriteAllTextAsync(path, "alsifaihsdfiuahwepouihagjah");
                await FileUploader.UploadOneFile(path, adminClient, $"sheetmusic/sets/{testSet.Id}/parts/{part.Name}/content");
            }

            var set = await adminClient.GetAsync($"sheetmusic/sets/{testSet.Id}/parts");
        }
    }
}
