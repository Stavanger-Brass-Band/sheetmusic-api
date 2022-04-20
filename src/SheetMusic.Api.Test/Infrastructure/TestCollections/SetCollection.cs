using Xunit;

namespace SheetMusic.Api.Test.Infrastructure.TestCollections;

[CollectionDefinition(Collections.Set)]
public class SetCollection : ICollectionFixture<SheetMusicWebAppFactory>
{
    //only for marking collections
}
