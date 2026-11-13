using LearningMap.Domain.Entities;

namespace LearningMap.Application.Interfaces.Services;

public interface IAuthService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedHash);
    (string Token, DateTimeOffset ExpiresAt) GenerateJwtToken(AppUser user);
}

/// <summary>Storage for employee images. Implemented with Azure Blob Storage (Azurite locally).</summary>
public interface IImageStore
{
    Task EnsureContainerExistsAsync(CancellationToken cancellationToken = default);

    /// <returns>The blob name that was written.</returns>
    Task<string> UploadAsync(string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>A short-lived, read-only URL (SAS) a browser can open directly.</summary>
    string GetReadUrl(string blobName);
}

/// <summary>Who is calling the API right now (read from the JWT). Null when anonymous.</summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
}
