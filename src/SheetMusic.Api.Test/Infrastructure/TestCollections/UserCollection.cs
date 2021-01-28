using Xunit;

namespace SheetMusic.Api.Test.Infrastructure.TestCollections
{
    [CollectionDefinition(Collections.User)]
    public class UserCollection : ICollectionFixture<SheetMusicWebAppFactory>
    {
        //only for defining collection 
    }
}
