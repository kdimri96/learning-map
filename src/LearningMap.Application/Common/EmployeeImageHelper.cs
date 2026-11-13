using LearningMap.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LearningMap.Application.Common;

/// <summary>Image logic shared by the Create / Update / Delete employee handlers.</summary>
public static class EmployeeImageHelper
{
    public static async Task<string> UploadAsync(IImageStore imageStore, Guid employeeGuid, IFormFile image, CancellationToken cancellationToken)
    {
        // The validator already checked the signature, so this is never null here.
        var contentType = ImageSignature.DetectContentType(image)
            ?? throw new InvalidOperationException("Image passed validation but has an unknown signature.");

        // A NEW random name on every upload: browsers/CDNs never show a cached old picture,
        // and the old blob can be deleted safely after the database update succeeds.
        var blobName = $"employees/{employeeGuid}/{Guid.NewGuid():N}{ImageSignature.ExtensionFor(contentType)}";

        await using var stream = image.OpenReadStream();
        return await imageStore.UploadAsync(blobName, stream, contentType, cancellationToken);
    }

    /// <summary>
    /// "Best effort" delete: if it fails we only log. A leftover file costs a little storage;
    /// failing the whole request after the database already changed would be worse.
    /// </summary>
    public static async Task TryDeleteAsync(IImageStore imageStore, string? blobName, ILogger logger)
    {
        if (string.IsNullOrEmpty(blobName)) return;
        try
        {
            await imageStore.DeleteIfExistsAsync(blobName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete image blob {BlobName}", blobName);
        }
    }
}
