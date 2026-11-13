using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using Microsoft.Extensions.Options;

namespace LearningMap.Infrastructure.Services;

/// <summary>
/// Stores images in Azure Blob Storage, the same SDK SafeSpace uses. Locally it talks to Azurite,
/// the official emulator; switching to real Azure only means changing the connection string.
/// </summary>
public class AzureBlobImageStore(BlobServiceClient blobServiceClient, IOptions<BlobStorageSettings> options) : IImageStore
{
    private readonly BlobStorageSettings _settings = options.Value;
    private BlobContainerClient Container => blobServiceClient.GetBlobContainerClient(_settings.ContainerName);

    /// <summary>
    /// Called once at startup. (SafeSpace calls CreateIfNotExists in the constructor with
    /// .GetAwaiter().GetResult(), which blocks a thread on every request; startup is the better place.)
    /// </summary>
    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default) =>
        await Container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

    public async Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var blob = Container.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, cancellationToken);
        return blobName;
    }

    public async Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default) =>
        await Container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);

    /// <summary>
    /// The container is PRIVATE. A SAS (Shared Access Signature) is a signed URL that grants
    /// read access to one blob for a limited time; no API call is needed to view the image.
    /// </summary>
    public string GetReadUrl(string blobName)
    {
        var blob = Container.GetBlobClient(blobName);
        if (!blob.CanGenerateSasUri)
        {
            // Happens only if the client was built without an account key (e.g. Managed Identity).
            return blob.Uri.ToString();
        }
        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(_settings.SasExpiryMinutes)).ToString();
    }
}
