using Bogus;
using SheetMusic.Api.Test.Models;
using System;

namespace SheetMusic.Api.Test.Utility
{
    internal static class FakerFactory
    {
        private const string Norwegian = "nb_NO";

        internal static Faker<PutSetModel> CreateSetFaker()
        {
            var faker = new Faker<PutSetModel>(Norwegian)
                .RuleFor(s => s.OriginatingId, Guid.NewGuid())
                .RuleFor(s => s.Title, f => f.Lorem.Sentence())
                .RuleFor(s => s.Composer, f => f.Person.FullName)
                .RuleFor(s => s.Arranger, f => f.Person.FullName)
                .RuleFor(s => s.SoleSellingAgent, f => f.Company.CompanyName())
                .RuleFor(s => s.BorrowedFrom, f => f.Company.CompanyName());

            return faker;
        }

        internal static Faker<PutPartModel> CreatePartFaker()
        {
            var faker = new Faker<PutPartModel>(Norwegian)
                .RuleFor(p => p.Name, f => f.Lorem.Word())
                .RuleFor(p => p.SortOrder, f => f.Random.Int(1, 99))
                .RuleFor(p => p.Indexable, f => f.Random.Bool());

            return faker;
        }
    }
}
