using AutoMapper;
using FluentValidation;
using LearningMap.Application.Dtos;
using LearningMap.Application.Interfaces.Repositories;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Application.Mappings;
using LearningMap.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LearningMap.Application.Queries;

// ---------- GET /employees?search=&department=&pageNumber=&pageSize= ----------

public class GetEmployeesQuery : IRequest<ApiResponseData>
{
    /// <summary>Matches employee code (EMP-00001), first name, last name, email or designation.</summary>
    public string? Search { get; set; }
    public string? Department { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public class GetEmployeesQueryHandler(IUnitOfWork unitOfWork, IImageStore imageStore, IMapper mapper)
    : IRequestHandler<GetEmployeesQuery, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await unitOfWork.Employees.GetPagedAsync(
            request.Search, request.Department, request.PageNumber, request.PageSize, cancellationToken);

        var result = new PagedResult<EmployeeDto>
        {
            Items = items.Select(e => mapper.ToDto(e, imageStore)).ToList(),
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = total,
        };
        return ApiResponseData.Success(result);
    }
}

// ---------- GET /employees/{guid} ----------

public record GetEmployeeByIdQuery(Guid EmployeeGuid) : IRequest<ApiResponseData>;

public class GetEmployeeByIdQueryHandler(IUnitOfWork unitOfWork, IImageStore imageStore, IMapper mapper)
    : IRequestHandler<GetEmployeeByIdQuery, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var employee = await unitOfWork.Employees.GetByGuidAsync(request.EmployeeGuid, cancellationToken);
        return employee is null
            ? ApiResponseData.Fail(StatusCodes.Status404NotFound, "Employee not found.")
            : ApiResponseData.Success(mapper.ToDto(employee, imageStore));
    }
}

// ---------- GET /employees/department-summary  (Dapper + stored procedure) ----------

public record GetDepartmentSummaryQuery : IRequest<ApiResponseData>;

public class GetDepartmentSummaryQueryHandler(IReportRepository reportRepository)
    : IRequestHandler<GetDepartmentSummaryQuery, ApiResponseData>
{
    public async Task<ApiResponseData> Handle(GetDepartmentSummaryQuery request, CancellationToken cancellationToken) =>
        ApiResponseData.Success(await reportRepository.GetDepartmentSummaryAsync(cancellationToken));
}
