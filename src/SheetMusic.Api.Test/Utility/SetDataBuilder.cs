using FluentAssertions;
using Newtonsoft.Json;
using SheetMusic.Api.Test.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SheetMusic.Api.Test.Utility
{
    internal class SetDataBuilder
    {
        private readonly HttpClient httpClient;
        private readonly List<PutSetModel> sets = new List<PutSetModel>();

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
                var body = await response.Content.ReadAsStringAsync();
                var apiSet = JsonConvert.DeserializeObject<ApiSet>(body);
                apiSet.OriginatingId = set.OriginatingId;
                AssertPropsAreEqual(set, apiSet);

                createdSets.Add(apiSet);
            }

            return createdSets;
        }

        internal PutSetModel GetRequestSet(Guid originatingId)
        {
            return sets.Single(s => s.OriginatingId == originatingId);
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
}
