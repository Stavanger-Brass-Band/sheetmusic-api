using Azure.Storage;
using Azure.Storage.Blobs;
using System;
using System.Threading.Tasks;
using Testcontainers.Azurite;
using Xunit;

namespace SheetMusic.Api.Test.Infrastructure;

/// <summary>
/// Spins up a real Azurite container (via Testcontainers) shared across every test in the "Azurite"
/// collection, so tests can exercise the real <c>BlobClient</c> and Data Protection Azure Storage
/// persistence against an actual blob storage endpoint instead of a mock - see issues #250 and #235.
/// Requires Docker; GitHub-hosted runners provide it by default, so this needs no extra CI setup.
/// </summary>
public class AzuriteFixture : IAsyncLifetime
{
    // --skipApiVersionCheck: the pinned Azurite image doesn't recognize the x-ms-version header sent by
    // the current Azure.Storage.Blobs SDK (which advertises a newer REST API version than Azurite's
    // hard-coded allow-list understands), so every request would otherwise fail with a 400
    // InvalidHeaderValue before this fixture's tests get a chance to run anything.
    private readonly AzuriteContainer container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.28.0")
        .WithCommand("--skipApiVersionCheck")
        .Build();

    public Uri BlobEndpoint => new(container.GetBlobEndpoint());

    public string ConnectionString => container.GetConnectionString();

    public StorageSharedKeyCredential Credential { get; } =
        new(AzuriteBuilder.AccountName, AzuriteBuilder.AccountKey);

    /// <summary>A <see cref="BlobServiceClient"/> built from a plain connection string - the local-emulator path.</summary>
    public BlobServiceClient CreateClientFromConnectionString() => new(container.GetConnectionString());

    /// <summary>
    /// A <see cref="BlobServiceClient"/> built from a service URI plus a credential - stands in for the
    /// published/managed-identity path. Azurite doesn't support Azure AD auth, so a shared-key
    /// credential is used here to exercise the same "URI, not connection string" construction path that
    /// the original <c>BlobClient</c> could not handle (issue #249).
    /// </summary>
    public BlobServiceClient CreateClientFromServiceUri() => new(BlobEndpoint, Credential);

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}
