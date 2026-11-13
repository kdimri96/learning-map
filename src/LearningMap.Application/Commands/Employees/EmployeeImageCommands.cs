using AutoMapper;
using FluentValidation;
using LearningMap.Application.Common;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Application.Mappings;
using LearningMap.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;

namespace LearningMap.Application.Commands.Employees;

// Image-only operations: change or remove the picture WITHOUT resending all the employee's fields.
// (Similar to SafeSpace's UploadProfilePicCommand.)

// ---------- PUT /employees/{guid}/image ----------

public class UploadEmployeeImageCommand : IRequest<ApiResponseData>
{
    [BindNever]
    public Guid EmployeeGuid { get; set; }

    public IFormFile? Image { get; set; }
}

public class UploadEmployeeImageCommandValidator : AbstractValidator<UploadEmployeeImageCommand>
{
    public UploadEmployeeImageCommandValidator()
    {
        RuleFor(x => x.EmployeeGuid).NotEmpty();
        RuleFor(x => x.Image).NotNull().WithMessage("Choose an image to upload.");
        When(x => x.Image is not null, () => RuleFor(x => x.Image!).SetValidator(new ImageFileValidator()));
    }
}

public class UploadEmployeeImageCommandHandler(
    IUnitOfWork unitOfWork,
    IImageStore imageStore,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<UploadEmployeeImageCommandHandler> logger)
    : IRequestHandler<UploadEmployeeImageCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(UploadEmployeeImageCommand request, CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByGuidAsync(request.EmployeeGuid, cancellationToken);
        if (employee is null)
        {
            return ApiResponseData.Fail(StatusCodes.Status404NotFound, "Employee not found.");
        }

        var oldBlobName = employee.ProfileImageBlobName;
        var newBlobName = await EmployeeImageHelper.UploadAsync(imageStore, employee.EmployeeGuid, request.Image!, cancellationToken);

        employee.ProfileImageBlobName = newBlobName;
        employee.ImageUploadedDate = timeProvider.GetUtcNow();
        unitOfWork.Employees.Update(employee);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await EmployeeImageHelper.TryDeleteAsync(imageStore, newBlobName, logger);
            throw;
        }

        await EmployeeImageHelper.TryDeleteAsync(imageStore, oldBlobName, logger);
        return ApiResponseData.Success(mapper.ToDto(employee, imageStore));
    }
}

// ---------- DELETE /employees/{guid}/image ----------

public record RemoveEmployeeImageCommand(Guid EmployeeGuid) : IRequest<ApiResponseData>;

public class RemoveEmployeeImageCommandHandler(
    IUnitOfWork unitOfWork,
    IImageStore imageStore,
    IMapper mapper,
    ILogger<RemoveEmployeeImageCommandHandler> logger)
    : IRequestHandler<RemoveEmployeeImageCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(RemoveEmployeeImageCommand request, CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByGuidAsync(request.EmployeeGuid, cancellationToken);
        if (employee is null)
        {
            return ApiResponseData.Fail(StatusCodes.Status404NotFound, "Employee not found.");
        }
        if (employee.ProfileImageBlobName is null)
        {
            return ApiResponseData.Success(mapper.ToDto(employee, imageStore)); // nothing to do: idempotent
        }

        var oldBlobName = employee.ProfileImageBlobName;
        employee.ProfileImageBlobName = null;
        employee.ImageUploadedDate = null;
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await EmployeeImageHelper.TryDeleteAsync(imageStore, oldBlobName, logger);
        return ApiResponseData.Success(mapper.ToDto(employee, imageStore));
    }
}
