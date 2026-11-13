using AutoMapper;
using LearningMap.Application.Common;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Application.Mappings;
using LearningMap.Domain.Common;
using LearningMap.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LearningMap.Application.Commands.Employees;

public class CreateEmployeeCommand : EmployeeCommandBase, IRequest<ApiResponseData>;

public class CreateEmployeeCommandValidator(TimeProvider timeProvider)
    : EmployeeCommandValidatorBase<CreateEmployeeCommand>(timeProvider);

public class CreateEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    IImageStore imageStore,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<CreateEmployeeCommandHandler> logger)
    : IRequestHandler<CreateEmployeeCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await unitOfWork.Employees.ExistsAsync(e => e.Email == email, cancellationToken))
        {
            return ApiResponseData.Fail(StatusCodes.Status409Conflict, "An employee with this email already exists.");
        }

        // Two-type overload: map using the BASE type's map (Command -> Entity).
        var employee = mapper.Map<EmployeeCommandBase, Employee>(request);
        employee.EmployeeGuid = Guid.NewGuid();

        if (request.Image is not null)
        {
            employee.ProfileImageBlobName = await EmployeeImageHelper.UploadAsync(imageStore, employee.EmployeeGuid, request.Image, cancellationToken);
            employee.ImageUploadedDate = timeProvider.GetUtcNow();
        }

        await unitOfWork.Employees.AddAsync(employee, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // The DB insert failed, so the image we just uploaded is an orphan; remove it, then re-throw.
            await EmployeeImageHelper.TryDeleteAsync(imageStore, employee.ProfileImageBlobName, logger);
            throw;
        }

        logger.LogInformation("Employee {EmployeeGuid} created", employee.EmployeeGuid);
        return ApiResponseData.Success(mapper.ToDto(employee, imageStore), StatusCodes.Status201Created);
    }
}
