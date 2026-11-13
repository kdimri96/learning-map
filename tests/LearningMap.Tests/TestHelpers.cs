using LearningMap.Domain.Common;
using LearningMap.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LearningMap.Tests;

internal static class TestHelpers
{
    public static readonly JwtSettings Jwt = new()
    {
        IssuerSigningKey = "unit-test-signing-key-that-is-at-least-32-chars",
        ValidIssuer = "test-issuer",
        ValidAudience = "test-audience",
        TokenExpirationInMinutes = 30,
    };

    public static AuthService CreateAuthService() => new(Options.Create(Jwt), TimeProvider.System);

    // Smallest valid file headers for each format, padded so they are not "empty".
    public static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0, 0, 0, 0, 0];
    public static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0];
    public static readonly byte[] NotAnImage = "MZ this is pretending to be an exe"u8.ToArray();

    public static IFormFile FormFile(byte[] content, string fileName, long? lengthOverride = null)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, lengthOverride ?? content.Length, "Image", fileName);
    }
}
