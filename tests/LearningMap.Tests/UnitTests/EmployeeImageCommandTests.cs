using AutoMapper;
using FluentAssertions;
using FluentValidation.TestHelper;
using LearningMap.Application.Commands.Employees;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Application.Mappings;
using LearningMap.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LearningMap.Tests.UnitTests;

public class EmployeeImageCommandTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IImageStore> _imageStore = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    public EmployeeImageCommandTests()
    {
        _unitOfWork.Setup(u => u.Employees).Returns(_employees.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _imageStore.Setup(s => s.GetReadUrl(It.IsAny<string>())).Returns<string>(n => $"https://blob/{n}");
    }

    [Fact]
    public void Upload_requires_an_image()
    {
        new UploadEmployeeImageCommandValidator()
            .TestValidate(new UploadEmployeeImageCommand { EmployeeGuid = Guid.NewGuid() })
            .ShouldHaveValidationErrorFor(x => x.Image);
    }

    [Fact]
    public async Task Upload_replaces_image_sets_upload_date_and_deletes_old_file()
    {
        var employee = new Employee { EmployeeGuid = Guid.NewGuid(), EmployeeCode = "EMP-00001", ProfileImageBlobName = "employees/1/old.png" };
        _employees.Setup(r => r.GetByGuidAsync(employee.EmployeeGuid, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _imageStore.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("employees/1/new.png");

        var handler = new UploadEmployeeImageCommandHandler(_unitOfWork.Object, _imageStore.Object, _mapper, TimeProvider.System,
            NullLogger<UploadEmployeeImageCommandHandler>.Instance);

        var result = await handler.Handle(new UploadEmployeeImageCommand
        {
            EmployeeGuid = employee.EmployeeGuid,
            Image = TestHelpers.FormFile(TestHelpers.PngBytes, "new.png"),
        }, CancellationToken.None);

        result.StatusCode.Should().Be(200);
        var dto = result.Data.Should().BeOfType<EmployeeDto>().Subject;
        dto.EmployeeCode.Should().Be("EMP-00001");
        dto.HasImage.Should().BeTrue();
        dto.ImageUploadedDate.Should().NotBeNull();
        _imageStore.Verify(s => s.DeleteIfExistsAsync("employees/1/old.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_clears_image_fields_and_deletes_file()
    {
        var employee = new Employee
        {
            EmployeeGuid = Guid.NewGuid(), ProfileImageBlobName = "employees/1/pic.png", ImageUploadedDate = DateTimeOffset.UtcNow,
        };
        _employees.Setup(r => r.GetByGuidAsync(employee.EmployeeGuid, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var handler = new RemoveEmployeeImageCommandHandler(_unitOfWork.Object, _imageStore.Object, _mapper,
            NullLogger<RemoveEmployeeImageCommandHandler>.Instance);

        var result = await handler.Handle(new RemoveEmployeeImageCommand(employee.EmployeeGuid), CancellationToken.None);

        result.StatusCode.Should().Be(200);
        employee.ProfileImageBlobName.Should().BeNull();
        employee.ImageUploadedDate.Should().BeNull();
        result.Data.Should().BeOfType<EmployeeDto>().Which.HasImage.Should().BeFalse();
        _imageStore.Verify(s => s.DeleteIfExistsAsync("employees/1/pic.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_for_unknown_employee_returns_404()
    {
        _employees.Setup(r => r.GetByGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);
        var handler = new RemoveEmployeeImageCommandHandler(_unitOfWork.Object, _imageStore.Object, _mapper,
            NullLogger<RemoveEmployeeImageCommandHandler>.Instance);

        (await handler.Handle(new RemoveEmployeeImageCommand(Guid.NewGuid()), CancellationToken.None)).StatusCode.Should().Be(404);
    }
}
