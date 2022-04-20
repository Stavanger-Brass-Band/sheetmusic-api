using Xunit;

namespace SheetMusic.Api.Test.Infrastructure.TestCollections;

[CollectionDefinition(Collections.Part)]
public class PartCollection : ICollectionFixture<SheetMusicWebAppFactory>
{
    //for defining collection
}
