using LearningMap.Application.Common;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LearningMap.Application.Commands.Employees;

public record DeleteEmployeeCommand(Guid EmployeeGuid) : IRequest<ApiResponseData>;

/// <summary>
/// Soft delete (same approach as SafeSpace's DeleteUserCommand): set IsDeleted = true instead of
/// removing the row, so history/audit data survives. The image file itself is removed to free storage.
/// </summary>
public class DeleteEmployeeCommandHandler(
    IUnitOfWork unitOfWork,
    IImageStore imageStore,
    ILogger<DeleteEmployeeCommandHandler> logger)
    : IRequestHandler<DeleteEmployeeCommand, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(DeleteEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByGuidAsync(request.EmployeeGuid, cancellationToken);
        if (employee is null)
        {
            return ApiResponseData.Fail(StatusCodes.Status404NotFound, "Employee not found.");
        }

        var blobName = employee.ProfileImageBlobName;
        employee.IsDeleted = true;
        employee.ProfileImageBlobName = null;
        employee.ImageUploadedDate = null;
        unitOfWork.Employees.Update(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await EmployeeImageHelper.TryDeleteAsync(imageStore, blobName, logger);

        logger.LogInformation("Employee {EmployeeGuid} soft-deleted", employee.EmployeeGuid);
        return ApiResponseData.Success("Employee deleted successfully.");
    }
}
