using FluentAssertions;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.Authentication;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using SheetMusic.Api.Test.Utility;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace SheetMusic.Api.Test.Tests
{
    [Collection(Collections.Part)]
    public class PartTests : IClassFixture<SheetMusicWebAppFactory>
    {
        private readonly SheetMusicWebAppFactory factory;

        public PartTests(SheetMusicWebAppFactory factory)
        {
            this.factory = factory;
        }

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
    }
}
