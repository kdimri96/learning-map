using FluentValidation;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LearningMap.Application.Commands.Auth;

// CQRS: a "command" changes something, a "query" only reads.
// The command (data) + its validator + its handler (logic) live together in one file, like SafeSpace.

public class LoginCommand : IRequest<ApiResponseData>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public class LoginCommandHandler(IUnitOfWork unitOfWork, IAuthService authService, TimeProvider timeProvider)
    : IRequestHandler<LoginCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await unitOfWork.Users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);

        // Same message for "no such user" and "wrong password": never tell an attacker which emails exist.
        if (user is null || !user.IsActive || !authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return ApiResponseData.Fail(StatusCodes.Status401Unauthorized, "Invalid email or password.");
        }

        user.LastLoginDate = timeProvider.GetUtcNow();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = authService.GenerateJwtToken(user);
        return ApiResponseData.Success(new LoginResponseDto(token, expiresAt, user.UserGuid, user.FullName, user.Email, user.Role));
    }
}
