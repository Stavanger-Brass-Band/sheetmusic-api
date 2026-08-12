using FluentAssertions;
using SheetMusic.Api.Test.Sets.Models;
using ApiSet = SheetMusic.Api.Sets.ViewModels.ApiSet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Utility;

internal class SetDataBuilder
{
    private readonly HttpClient httpClient;
    private readonly List<PutSetModel> sets = new List<PutSetModel>();
    private readonly Dictionary<Guid, PutSetModel> setsById = new Dictionary<Guid, PutSetModel>();

    internal SetDataBuilder(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    internal async Task<ApiSet> ProvisionSingleSetAsync()
    {
        var createdSets = await WithSets(1).ProvisionAsync();
        return createdSets.Single();
    }

    internal SetDataBuilder WithSets(int numberOfSets)
    {
        var fakeSets = FakerFactory.CreateSetFaker().Generate(numberOfSets);
        sets.AddRange(fakeSets);

        return this;
    }

    internal async Task<List<ApiSet>> ProvisionAsync()
    {
        var createdSets = new List<ApiSet>();

        foreach (var set in sets)
        {
            var response = await httpClient.PostAsJsonAsync($"sheetmusic/sets", set);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiSet = await response.Content.ReadFromJsonAsync<ApiSet>(JsonDefaults.Options);
            apiSet.Should().NotBeNull();
            setsById.Add(apiSet!.Id, set);
            AssertPropsAreEqual(set, apiSet);

            createdSets.Add(apiSet);
        }

        return createdSets;
    }

    internal PutSetModel GetRequestSet(Guid setId)
    {
        return setsById[setId];
    }
    
    private static void AssertPropsAreEqual(PutSetModel set, ApiSet apiSet)
    {
        apiSet.Title.Should().Be(set.Title);
        apiSet.Composer.Should().Be(set.Composer);
        apiSet.Arranger.Should().Be(set.Arranger);
        apiSet.SoleSellingAgent.Should().Be(set.SoleSellingAgent);
        apiSet.MissingParts.Should().Be(set.MissingParts);
        apiSet.RecordingUrl.Should().Be(set.RecordingUrl);
        apiSet.BorrowedFrom.Should().Be(set.BorrowedFrom);
    }
}
