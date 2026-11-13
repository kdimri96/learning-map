using FluentAssertions;
using FluentValidation.TestHelper;
using LearningMap.Application.Commands.Employees;

namespace LearningMap.Tests.UnitTests;

public class EmployeeValidatorTests
{
    private readonly CreateEmployeeCommandValidator _validator = new(TimeProvider.System);

    private static CreateEmployeeCommand ValidCommand() => new()
    {
        FirstName = "Asha",
        LastName = "Rao",
        Email = "asha.rao@example.com",
        PhoneNumber = "+919876543210",
        Department = "Engineering",
        Designation = "Backend Developer",
        DateOfJoining = DateTime.UtcNow.Date.AddYears(-1),
    };

    [Fact]
    public void Valid_command_has_no_errors()
    {
        _validator.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_first_name_and_bad_email_are_reported()
    {
        var cmd = ValidCommand();
        cmd.FirstName = "";
        cmd.Email = "not-an-email";

        var result = _validator.TestValidate(cmd);

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Future_joining_date_is_rejected()
    {
        var cmd = ValidCommand();
        cmd.DateOfJoining = DateTime.UtcNow.Date.AddDays(5);

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DateOfJoining);
    }

    [Fact]
    public void Real_png_image_is_accepted()
    {
        var cmd = ValidCommand();
        cmd.Image = TestHelpers.FormFile(TestHelpers.PngBytes, "photo.png");

        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Exe_renamed_to_png_is_rejected_by_signature_check()
    {
        var cmd = ValidCommand();
        cmd.Image = TestHelpers.FormFile(TestHelpers.NotAnImage, "totally-a-photo.png");

        _validator.TestValidate(cmd).Errors.Should().Contain(e => e.ErrorMessage.Contains("not a valid JPEG, PNG or WebP"));
    }

    [Fact]
    public void Wrong_extension_is_rejected()
    {
        var cmd = ValidCommand();
        cmd.Image = TestHelpers.FormFile(TestHelpers.JpegBytes, "photo.gif");

        _validator.TestValidate(cmd).Errors.Should().Contain(e => e.ErrorMessage.Contains("Only .jpg"));
    }

    [Fact]
    public void Image_over_2MB_is_rejected()
    {
        var cmd = ValidCommand();
        cmd.Image = TestHelpers.FormFile(TestHelpers.PngBytes, "big.png", lengthOverride: ImageFileValidator.MaxBytes + 1);

        _validator.TestValidate(cmd).Errors.Should().Contain(e => e.ErrorMessage.Contains("2 MB"));
    }

    [Fact]
    public void Update_rejects_new_image_together_with_RemoveImage()
    {
        var validator = new UpdateEmployeeCommandValidator(TimeProvider.System);
        var cmd = new UpdateEmployeeCommand
        {
            EmployeeGuid = Guid.NewGuid(),
            FirstName = "Asha", LastName = "Rao", Email = "asha@example.com",
            Department = "Engineering", Designation = "Dev",
            DateOfJoining = DateTime.UtcNow.Date,
            Image = TestHelpers.FormFile(TestHelpers.PngBytes, "p.png"),
            RemoveImage = true,
        };

        validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.RemoveImage);
    }
}
