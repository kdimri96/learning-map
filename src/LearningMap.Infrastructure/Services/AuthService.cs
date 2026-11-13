using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using LearningMap.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LearningMap.Infrastructure.Services;

public class AuthService(IOptions<JwtSettings> jwtOptions, TimeProvider timeProvider) : IAuthService
{
    // Same Argon2id parameters as SafeSpace's AuthService, so hashes are comparable.
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 8;
    private const int MemorySizeKb = 65536; // 64 MB
    private const int Iterations = 4;

    private readonly JwtSettings _jwt = jwtOptions.Value;

    /// <summary>
    /// Argon2id is deliberately SLOW and memory-hungry, so brute-forcing a leaked hash is expensive.
    /// A random salt makes two users with the same password get different hashes.
    /// Stored format: Base64( salt[16] + hash[32] ).
    /// </summary>
    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);
        byte[] saltAndHash = [.. salt, .. hash];
        return Convert.ToBase64String(saltAndHash);
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        byte[] stored;
        try
        {
            stored = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }
        if (stored.Length != SaltSize + HashSize) return false;

        var salt = stored.AsSpan(0, SaltSize).ToArray();
        var expected = stored.AsSpan(SaltSize);
        var actual = ComputeHash(password, salt);

        // Constant-time compare: takes the same time whether the first or last byte differs,
        // so an attacker cannot learn the hash byte by byte from response timings.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public (string Token, DateTimeOffset ExpiresAt) GenerateJwtToken(AppUser user)
    {
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(_jwt.TokenExpirationInMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.IssuerSigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.ValidIssuer,
            Audience = _jwt.ValidAudience,
            IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
            NotBefore = timeProvider.GetUtcNow().UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [AppClaimTypes.UserId] = user.UserId.ToString(),
                [AppClaimTypes.Email] = user.Email,
                [AppClaimTypes.Name] = user.FullName,
                [AppClaimTypes.Role] = user.Role,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(), // unique token id
            },
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySizeKb,
            Iterations = Iterations,
        };
        return argon2.GetBytes(HashSize);
    }
}
