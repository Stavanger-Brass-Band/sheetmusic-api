using Xunit;

namespace SheetMusic.Api.Test.Infrastructure.TestCollections;

[CollectionDefinition(Collections.Azurite)]
public class AzuriteCollection : ICollectionFixture<AzuriteFixture>
{
    // Shares one Azurite container across every test that needs a real blob storage backend.
}
