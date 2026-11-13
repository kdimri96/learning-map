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

/// <summary>PUT = full replace of the employee's fields. Image: send a new one, set RemoveImage, or neither to keep it.</summary>
public class UpdateEmployeeCommand : EmployeeCommandBase, IRequest<ApiResponseData>
{
    /// <summary>Comes from the URL (/employees/{guid}), not the form; the controller sets it.</summary>
    [BindNever]
    public Guid EmployeeGuid { get; set; }

    public bool RemoveImage { get; set; }
}

public class UpdateEmployeeCommandValidator : EmployeeCommandValidatorBase<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator(TimeProvider timeProvider) : base(timeProvider)
    {
        RuleFor(x => x.EmployeeGuid).NotEmpty();
        RuleFor(x => x.RemoveImage)
            .Equal(false).When(x => x.Image is not null)
            .WithMessage("Send either a new Image or RemoveImage=true, not both.");
    }
}

public class UpdateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    IImageStore imageStore,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<UpdateEmployeeCommandHandler> logger)
    : IRequestHandler<UpdateEmployeeCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByGuidAsync(request.EmployeeGuid, cancellationToken);
        if (employee is null)
        {
            return ApiResponseData.Fail(StatusCodes.Status404NotFound, "Employee not found.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await unitOfWork.Employees.ExistsAsync(e => e.Email == email && e.EmployeeId != employee.EmployeeId, cancellationToken))
        {
            return ApiResponseData.Fail(StatusCodes.Status409Conflict, "Another employee already uses this email.");
        }

        // Copy the form values onto the tracked entity. EF notices the changes; no Update() call needed,
        // but we call it anyway to mirror the repository pattern used in SafeSpace.
        mapper.Map<EmployeeCommandBase, Domain.Entities.Employee>(request, employee);

        var oldBlobName = employee.ProfileImageBlobName;
        string? newBlobName = null;

        if (request.Image is not null)
        {
            newBlobName = await EmployeeImageHelper.UploadAsync(imageStore, employee.EmployeeGuid, request.Image, cancellationToken);
            employee.ProfileImageBlobName = newBlobName;
            employee.ImageUploadedDate = timeProvider.GetUtcNow();
        }
        else if (request.RemoveImage)
        {
            employee.ProfileImageBlobName = null;
            employee.ImageUploadedDate = null;
        }

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

        // Only delete the OLD file after the database points at the new one.
        // Doing it before the save could leave a row pointing at a missing image.
        if (oldBlobName is not null && oldBlobName != employee.ProfileImageBlobName)
        {
            await EmployeeImageHelper.TryDeleteAsync(imageStore, oldBlobName, logger);
        }

        return ApiResponseData.Success(mapper.ToDto(employee, imageStore));
    }
}
