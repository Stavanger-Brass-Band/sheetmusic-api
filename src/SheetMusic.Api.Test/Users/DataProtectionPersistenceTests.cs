using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using SheetMusic.Api.BlobStorage;
using SheetMusic.Api.Test.Infrastructure;
using SheetMusic.Api.Test.Infrastructure.TestCollections;
using System;
using Xunit;

namespace SheetMusic.Api.Test.Users;

/// <summary>
/// Verifies the exact Data Protection configuration Program.cs applies - a custom
/// AzureBlobXmlRepository wired in via KeyManagementOptions.XmlRepository - actually persists the key
/// ring to blob storage rather than relying on the process's local filesystem. Without this (issue
/// #235), every Azure Container Apps scale-to-zero cold start would regenerate the key ring and
/// invalidate outstanding password-reset tokens (UsersController.ForgotPassword) well inside their
/// one-hour lifespan, and any scale-out would break validation across replicas.
/// </summary>
[Collection(Collections.Azurite)]
public class DataProtectionPersistenceTests(AzuriteFixture azurite)
{
    [Fact]
    public void KeyRingPersistsAcrossSimulatedAppRestart()
    {
        var containerName = $"data-protection-keys-{Guid.NewGuid():N}";
        var blobServiceClient = azurite.CreateClientFromConnectionString();
        var container = blobServiceClient.GetBlobContainerClient(containerName);

        // Two independent DI containers, each with their own in-memory key cache, standing in for two
        // separate application instances/restarts pointed at the same underlying blob - the exact
        // scenario that silently invalidates reset tokens without persistence.
        var protectorA = BuildProtector(container);
        var protectorB = BuildProtector(container);

        var protectedPayload = protectorA.Protect("password-reset-token");
        var unprotected = protectorB.Unprotect(protectedPayload);

        unprotected.Should().Be("password-reset-token");
    }

    private static IDataProtector BuildProtector(BlobContainerClient container)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("SheetMusic.Api.Test");
        services.Configure<KeyManagementOptions>(options =>
            options.XmlRepository = new AzureBlobXmlRepository(container, "keys.xml"));

        var provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
        return provider.CreateProtector("password-reset");
    }
}