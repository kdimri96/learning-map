using AutoMapper;
using FluentAssertions;
using LearningMap.Application.Commands.Employees;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Application.Mappings;
using LearningMap.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LearningMap.Tests.UnitTests;

public class EmployeeCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IImageStore> _imageStore = new();
    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    public EmployeeCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Employees).Returns(_employees.Object);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _imageStore.Setup(s => s.GetReadUrl(It.IsAny<string>())).Returns<string>(name => $"https://blob/{name}?sas");
    }

    [Fact]
    public void New_employee_has_null_EmployeeCode_so_the_database_generates_it()
    {
        // Regression test: with "" EF Core sends the empty string instead of using the DB default.
        new Employee().EmployeeCode.Should().BeNull();
    }

    [Fact]
    public void AutoMapper_configuration_is_valid()
    {
        // Fails if a destination property is neither mapped nor explicitly ignored: catches typos early.
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).AssertConfigurationIsValid();
    }

    [Fact]
    public async Task Create_uploads_image_saves_and_returns_201_with_image_url()
    {
        _employees.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _imageStore.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, Stream _, string _, CancellationToken _) => name);

        var handler = new CreateEmployeeCommandHandler(_unitOfWork.Object, _imageStore.Object, _mapper, TimeProvider.System, NullLogger<CreateEmployeeCommandHandler>.Instance);
        var cmd = new CreateEmployeeCommand
        {
            FirstName = " Asha ", LastName = "Rao", Email = "Asha@Example.com", Department = "Engineering",
            Designation = "Dev", DateOfJoining = DateTime.UtcNow.Date,
            Image = TestHelpers.FormFile(TestHelpers.PngBytes, "a.png"),
        };

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.StatusCode.Should().Be(201);
        var dto = result.Data.Should().BeOfType<EmployeeDto>().Subject;
        dto.FirstName.Should().Be("Asha");                // trimmed
        dto.Email.Should().Be("asha@example.com");         // lower-cased
        dto.ImageUrl.Should().StartWith("https://blob/employees/").And.EndWith(".png?sas");
        _employees.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_deletes_uploaded_image_when_database_save_fails()
    {
        _employees.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _imageStore.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("employees/x/new.png");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));

        var handler = new CreateEmployeeCommandHandler(_unitOfWork.Object, _imageStore.Object, _mapper, TimeProvider.System, NullLogger<CreateEmployeeCommandHandler>.Instance);
        var cmd = new CreateEmployeeCommand
        {
            FirstName = "A", LastName = "B", Email = "a@b.com", Department = "D", Designation = "X",
            DateOfJoining = DateTime.UtcNow.Date, Image = TestHelpers.FormFile(TestHelpers.PngBytes, "a.png"),
        };

        var act = () => handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _imageStore.Verify(s => s.DeleteIfExistsAsync("employees/x/new.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_unknown_employee_returns_404()
    {
        _employees.Setup(r => r.GetByGuidAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);
        var handler = new DeleteEmployeeCommandHandler(_unitOfWork.Object, _imageStore.Object, NullLogger<DeleteEmployeeCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteEmployeeCommand(Guid.NewGuid()), CancellationToken.None);

        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Delete_soft_deletes_and_removes_image_after_saving()
    {
        var employee = new Employee { EmployeeGuid = Guid.NewGuid(), ProfileImageBlobName = "employees/1/pic.png" };
        _employees.Setup(r => r.GetByGuidAsync(employee.EmployeeGuid, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        var handler = new DeleteEmployeeCommandHandler(_unitOfWork.Object, _imageStore.Object, NullLogger<DeleteEmployeeCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteEmployeeCommand(employee.EmployeeGuid), CancellationToken.None);

        result.StatusCode.Should().Be(200);
        employee.IsDeleted.Should().BeTrue();
        employee.ProfileImageBlobName.Should().BeNull();
        _imageStore.Verify(s => s.DeleteIfExistsAsync("employees/1/pic.png", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_with_new_image_deletes_the_old_image()
    {
        var employee = new Employee { EmployeeId = 7, EmployeeGuid = Guid.NewGuid(), ProfileImageBlobName = "employees/7/old.png" };
        _employees.Setup(r => r.GetByGuidAsync(employee.EmployeeGuid, It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        _employees.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Employee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _imageStore.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("employees/7/new.png");

        var handler = new UpdateEmployeeCommandHandler(_unitOfWork.Object, _imageStore.Object, _mapper, TimeProvider.System, NullLogger<UpdateEmployeeCommandHandler>.Instance);
        var cmd = new UpdateEmployeeCommand
        {
            EmployeeGuid = employee.EmployeeGuid, FirstName = "New", LastName = "Name", Email = "n@x.com",
            Department = "D", Designation = "X", DateOfJoining = DateTime.UtcNow.Date,
            Image = TestHelpers.FormFile(TestHelpers.PngBytes, "n.png"),
        };

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.StatusCode.Should().Be(200);
        employee.ProfileImageBlobName.Should().Be("employees/7/new.png");
        employee.FirstName.Should().Be("New");
        _imageStore.Verify(s => s.DeleteIfExistsAsync("employees/7/old.png", It.IsAny<CancellationToken>()), Times.Once);
    }
}
