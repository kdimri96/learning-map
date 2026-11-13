using FluentAssertions;
using LearningMap.Domain.Common;
using LearningMap.Domain.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LearningMap.Tests.UnitTests;

public class AuthServiceTests
{
    private readonly Infrastructure.Services.AuthService _sut = TestHelpers.CreateAuthService(); // sut = "system under test"

    [Fact]
    public void HashPassword_then_VerifyPassword_with_same_password_returns_true()
    {
        var hash = _sut.HashPassword("Correct#Horse1");

        _sut.VerifyPassword("Correct#Horse1", hash).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_with_wrong_password_returns_false()
    {
        var hash = _sut.HashPassword("Correct#Horse1");

        _sut.VerifyPassword("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void Same_password_hashed_twice_gives_different_hashes_because_of_random_salt()
    {
        _sut.HashPassword("Same#Pass1").Should().NotBe(_sut.HashPassword("Same#Pass1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-base64!!")]
    [InlineData("AAAA")] // valid Base64 but wrong length
    public void VerifyPassword_with_garbage_hash_returns_false_instead_of_throwing(string storedHash)
    {
        _sut.VerifyPassword("anything", storedHash).Should().BeFalse();
    }

    [Fact]
    public async Task GenerateJwtToken_creates_a_valid_signed_token_with_expected_claims()
    {
        var user = new AppUser { UserId = 42, Email = "asha@example.com", FullName = "Asha Rao", Role = AppRoles.Admin };

        var (token, expiresAt) = _sut.GenerateJwtToken(user);

        expiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(TestHelpers.Jwt.TokenExpirationInMinutes), TimeSpan.FromSeconds(10));

        // Validate exactly like the API does, to prove the token round-trips.
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = TestHelpers.Jwt.ValidIssuer,
            ValidAudience = TestHelpers.Jwt.ValidAudience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(TestHelpers.Jwt.IssuerSigningKey)),
        });

        result.IsValid.Should().BeTrue(result.Exception?.Message);
        result.Claims[AppClaimTypes.UserId].Should().Be("42");
        result.Claims[AppClaimTypes.Email].Should().Be("asha@example.com");
        result.Claims[AppClaimTypes.Role].Should().Be(AppRoles.Admin);
    }
}
