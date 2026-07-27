using FluentAssertions;
using SheetMusic.Api.Parts.RequestModels;
using SheetMusic.Api.Parts.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Utility;

public class PartDataBuilder(HttpClient httpClient)
{
    public List<PartRequest> PartRequests { get; private set; } = new List<PartRequest>();

    public async Task<ApiPart> ProvisionSinglePartAsync()
    {
        var createdData = await WithParts(1).ProvisionAsync();

        return createdData.Single();
    }

    public PartDataBuilder WithParts(int numberOfParts)
    {
        var items = FakerFactory.CreatePartFaker().Generate(numberOfParts);
        PartRequests.AddRange(items);
        return this;
    }

    public async Task<List<ApiPart>> ProvisionAsync()
    {
        var createdItems = new List<ApiPart>();

        foreach (var item in PartRequests)
        {
            var response = await httpClient.PostAsJsonAsync($"parts", item);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiPart = await response.Content.ReadFromJsonAsync<ApiPart>(JsonDefaults.Options);
            AssertPropsAreEqual(item, apiPart);
            createdItems.Add(apiPart!);
        }

        return createdItems;
    }

    public PartRequest? GetPartInput(string partName)
    {
        return PartRequests.FirstOrDefault(p => p.Name == partName);
    }

    private static void AssertPropsAreEqual(PartRequest item, ApiPart? apiPart)
    {
        apiPart.Should().NotBeNull();
        apiPart?.Name.Should().Be(item.Name);
        apiPart?.SortOrder.Should().Be(item.SortOrder);
        apiPart?.Indexable.Should().Be(item.Indexable ?? false);
    }
}
