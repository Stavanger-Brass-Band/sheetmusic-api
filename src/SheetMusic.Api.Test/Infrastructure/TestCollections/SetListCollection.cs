using Xunit;

namespace SheetMusic.Api.Test.Infrastructure.TestCollections;

/// <summary>
/// Dedicated collection for <c>GET /sheetmusic/sets</c> tests. Having its own collection gives the
/// tests an isolated in-memory database, so result counts and ordering are not affected by sets
/// created by other test classes.
/// </summary>
[CollectionDefinition(Collections.SetList)]
public class SetListCollection : ICollectionFixture<SheetMusicWebAppFactory>
{
    //only for marking collections
}
