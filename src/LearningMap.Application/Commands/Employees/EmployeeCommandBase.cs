using FluentValidation;
using LearningMap.Application.Common;
using Microsoft.AspNetCore.Http;

namespace LearningMap.Application.Commands.Employees;

/// <summary>
/// Fields shared by Create and Update. These commands arrive as multipart/form-data
/// (not JSON) because they can carry an image file.
/// </summary>
public abstract class EmployeeCommandBase
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime DateOfJoining { get; set; }

    /// <summary>Optional profile picture (JPEG, PNG or WebP, max 2 MB).</summary>
    public IFormFile? Image { get; set; }
}

/// <summary>Rules shared by Create and Update. Abstract, so FluentValidation's assembly scan skips it.</summary>
public abstract class EmployeeCommandValidatorBase<T> : AbstractValidator<T> where T : EmployeeCommandBase
{
    protected EmployeeCommandValidatorBase(TimeProvider timeProvider)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[1-9]\d{6,14}$").WithMessage("Phone number must be 7-15 digits, optionally starting with +.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Designation).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DateOfJoining)
            .NotEmpty()
            .Must(d => d.Date <= timeProvider.GetUtcNow().Date).WithMessage("Date of joining cannot be in the future.");

        When(x => x.Image is not null, () =>
        {
            RuleFor(x => x.Image!).SetValidator(new ImageFileValidator());
        });
    }
}

public class ImageFileValidator : AbstractValidator<IFormFile>
{
    public const long MaxBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public ImageFileValidator()
    {
        RuleFor(f => f.Length)
            .GreaterThan(0).WithMessage("Image file is empty.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("Image must be 2 MB or smaller.");

        RuleFor(f => f.FileName)
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("Only .jpg, .jpeg, .png and .webp images are allowed.");

        RuleFor(f => f)
            .Must(f => ImageSignature.DetectContentType(f) is not null)
            .WithName("Image")
            .WithMessage("File content is not a valid JPEG, PNG or WebP image.")
            .When(f => f.Length > 0);
    }
}
