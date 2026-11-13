using FluentAssertions;
using LearningMap.Application.Commands.Auth;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Entities;
using Moq;

namespace LearningMap.Tests.UnitTests;

/// <summary>
/// Unit tests with Moq: the handler gets FAKE repositories/services,
/// so the test needs no database and runs in milliseconds.
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly LoginCommandHandler _sut;

    private static readonly AppUser ActiveUser = new()
    {
        UserId = 1, UserGuid = Guid.NewGuid(), Email = "admin@test.local", FullName = "Admin",
        PasswordHash = "stored-hash", Role = "Admin", IsActive = true,
    };

    public LoginCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Users).Returns(_users.Object);
        _sut = new LoginCommandHandler(_unitOfWork.Object, _authService.Object, TimeProvider.System);
    }

    private static LoginCommand Command(string email = "admin@test.local", string password = "pw") =>
        new() { Email = email, Password = password };

    [Fact]
    public async Task Unknown_email_returns_401_and_never_saves()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.StatusCode.Should().Be(401);
        result.ResponseStatus.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Wrong_password_returns_401_with_same_message_as_unknown_email()
    {
        _users.Setup(r => r.GetByEmailAsync(ActiveUser.Email, It.IsAny<CancellationToken>())).ReturnsAsync(ActiveUser);
        _authService.Setup(a => a.VerifyPassword("bad", ActiveUser.PasswordHash)).Returns(false);

        var result = await _sut.Handle(Command(password: "bad"), CancellationToken.None);

        result.StatusCode.Should().Be(401);
        result.ErrorMessage.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task Email_is_normalised_before_lookup()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AppUser?)null);

        await _sut.Handle(Command(email: "  ADMIN@Test.Local "), CancellationToken.None);

        _users.Verify(r => r.GetByEmailAsync("admin@test.local", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Correct_credentials_return_200_with_token_and_record_last_login()
    {
        var user = new AppUser
        {
            UserId = ActiveUser.UserId, UserGuid = ActiveUser.UserGuid, Email = ActiveUser.Email, FullName = ActiveUser.FullName,
            PasswordHash = ActiveUser.PasswordHash, Role = ActiveUser.Role, IsActive = true,
        };
        _users.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _authService.Setup(a => a.VerifyPassword("pw", user.PasswordHash)).Returns(true);
        _authService.Setup(a => a.GenerateJwtToken(user)).Returns(("jwt-token", DateTimeOffset.UtcNow.AddHours(1)));

        var result = await _sut.Handle(Command(), CancellationToken.None);

        result.StatusCode.Should().Be(200);
        result.Data.Should().BeOfType<LoginResponseDto>().Which.Token.Should().Be("jwt-token");
        user.LastLoginDate.Should().NotBeNull();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
