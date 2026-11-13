using AutoMapper;
using FluentValidation;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using LearningMap.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LearningMap.Application.Commands.Auth;

/// <summary>Self sign-up. New users get the Viewer role (they can still add, edit and delete employees).</summary>
public class RegisterCommand : IRequest<ApiResponseData>
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().Length(2, 100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an upper-case letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lower-case letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a special character.");
    }
}

public class RegisterCommandHandler(IUnitOfWork unitOfWork, IAuthService authService, IMapper mapper)
    : IRequestHandler<RegisterCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await unitOfWork.Users.ExistsAsync(u => u.Email == email, cancellationToken))
        {
            return ApiResponseData.Fail(StatusCodes.Status409Conflict, "An account with this email already exists.");
        }

        var user = new AppUser
        {
            UserGuid = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = authService.HashPassword(request.Password),
            Role = AppRoles.Viewer,
            IsActive = true,
        };

        await unitOfWork.Users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ApiResponseData.Success(mapper.Map<UserSummaryDto>(user), StatusCodes.Status201Created);
    }
}
