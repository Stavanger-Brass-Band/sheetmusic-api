using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SheetMusic.Api.BlobStorage;

/// <summary>
/// Persists the ASP.NET Core Data Protection key ring to a single blob using the modern
/// <c>Azure.Storage.Blobs</c> SDK. Microsoft's own <c>Microsoft.AspNetCore.DataProtection.AzureStorage</c>
/// package is stuck on the legacy <c>Microsoft.Azure.Storage</c> SDK (last released for .NET Core 3.1) and
/// offers no overload compatible with a <see cref="BlobServiceClient"/> resolved via managed identity
/// (issue #235 / decision 9), hence this small custom repository instead - the pattern Microsoft's own
/// docs recommend for a custom key repository.
///
/// All keys are stored together as sibling &lt;key&gt; elements inside one XML document, matching the
/// format the built-in file system repository uses. <see cref="StoreElement"/> uses ETag-based optimistic
/// concurrency (conditional upload, retry-on-conflict) rather than a plain overwrite: multiple Container
/// Apps replicas can cold-start and each decide to generate a new key at roughly the same time, and a
/// blind read-modify-write would let one replica's upload silently clobber another's newly added key.
/// </summary>
public class AzureBlobXmlRepository(BlobContainerClient container, string blobName) : IXmlRepository
{
    private const int MaxConcurrencyRetries = 5;
    private static readonly XName RootElementName = "keys";

    public IReadOnlyCollection<XElement> GetAllElements() => GetAllElementsWithETag().Elements;

    public void StoreElement(XElement element, string friendlyName)
    {
        container.CreateIfNotExists();

        for (var attempt = 0; ; attempt++)
        {
            var (existingElements, etag) = GetAllElementsWithETag();
            var document = new XDocument(new XElement(RootElementName, existingElements.Append(element)));
            var conditions = etag is null
                ? new BlobRequestConditions { IfNoneMatch = ETag.All }
                : new BlobRequestConditions { IfMatch = etag };

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document.ToString()));

            try
            {
                container.GetBlobClient(blobName).Upload(stream, new BlobUploadOptions { Conditions = conditions });
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 412 && attempt < MaxConcurrencyRetries - 1)
            {
                // Another instance wrote a new key between our read and our upload; retry with a fresh
                // read so we merge with that key instead of overwriting it.
            }
        }
    }

    private (IReadOnlyCollection<XElement> Elements, ETag? ETag) GetAllElementsWithETag()
    {
        var blob = container.GetBlobClient(blobName);

        try
        {
            var content = blob.DownloadContent();
            var document = XDocument.Parse(content.Value.Content.ToString());
            return (document.Root?.Elements().ToList() ?? [], content.Value.Details.ETag);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return ([], null);
        }
    }
}
