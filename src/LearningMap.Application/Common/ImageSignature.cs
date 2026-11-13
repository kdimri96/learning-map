using Microsoft.AspNetCore.Http;

namespace LearningMap.Application.Common;

/// <summary>
/// Detects the real image type from the file's first bytes ("magic numbers").
/// The browser-supplied Content-Type and file name can be anything (an .exe renamed to .png),
/// so we never trust them for security decisions.
/// </summary>
public static class ImageSignature
{
    private static ReadOnlySpan<byte> Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> Jpeg => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> Riff => "RIFF"u8;
    private static ReadOnlySpan<byte> Webp => "WEBP"u8;

    /// <returns>"image/png", "image/jpeg", "image/webp", or null if it is not one of those.</returns>
    public static string? DetectContentType(IFormFile file)
    {
        Span<byte> header = stackalloc byte[12];
        using var stream = file.OpenReadStream();
        int read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        header = header[..read];

        if (header.StartsWith(Png)) return "image/png";
        if (header.StartsWith(Jpeg)) return "image/jpeg";
        if (header.Length >= 12 && header[..4].SequenceEqual(Riff) && header[8..12].SequenceEqual(Webp)) return "image/webp";
        return null;
    }

    public static string ExtensionFor(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg",
    };
}
