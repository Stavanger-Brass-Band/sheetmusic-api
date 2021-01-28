using FluentAssertions;
using Newtonsoft.Json;
using SheetMusic.Api.Test.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Utility
{
    public class PartDataBuilder
    {
        private readonly HttpClient httpClient;
        private List<PutPartModel> partRequests = new List<PutPartModel>();

        public PartDataBuilder(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<ApiPart> ProvisionSinglePartAsync()
        {
            var createdData = await WithParts(1).ProvisionAsync();

            return createdData.Single();
        }

        public PartDataBuilder WithParts(int numberOfParts)
        {
            var items = FakerFactory.CreatePartFaker().Generate(numberOfParts);
            partRequests.AddRange(items);
            return this;
        }

        public async Task<List<ApiPart>> ProvisionAsync()
        {
            var createdItems = new List<ApiPart>();

            foreach (var item in partRequests)
            {
                var response = await httpClient.PostAsJsonAsync($"parts", item);
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                var body = await response.Content.ReadAsStringAsync();
                var apiPart = JsonConvert.DeserializeObject<ApiPart>(body);
                AssertPropsAreEqual(item, apiPart);
                createdItems.Add(apiPart);
            }

            return createdItems;
        }

        private static void AssertPropsAreEqual(PutPartModel item, ApiPart apiPart)
        {
            apiPart.Name.Should().Be(item.Name);
            apiPart.SortOrder.Should().Be(item.SortOrder);
            apiPart.Indexable.Should().Be(item.Indexable ?? false);
        }
    }
}
