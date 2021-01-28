using Xunit;

namespace SheetMusic.Api.Test.Infrastructure.TestCollections
{
    [CollectionDefinition(Collections.Project)]
    public class ProjectCollection : ICollectionFixture<SheetMusicWebAppFactory>
    {
        //only for marking collections
    }
}
