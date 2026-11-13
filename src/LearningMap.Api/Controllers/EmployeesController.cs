using LearningMap.Application.Commands.Employees;
using LearningMap.Application.Queries;
using LearningMap.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningMap.Api.Controllers;

/// <summary>Employee CRUD. Every logged-in user (any role) can read, add, edit and delete employees.</summary>
[Route("employees")]
[Authorize]
public class EmployeesController(ISender mediator) : ApiControllerBase
{
    // Slightly above the 2 MB image limit to leave room for the other form fields.
    private const long MaxRequestBytes = 3 * 1024 * 1024;

    /// <summary>List employees with search + paging.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetEmployeesQuery query, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(query, cancellationToken));

    /// <summary>Head-count per department (Dapper + stored procedure).</summary>
    [HttpGet("department-summary")]
    public async Task<IActionResult> GetDepartmentSummary(CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(new GetDepartmentSummaryQuery(), cancellationToken));

    [HttpGet("{employeeGuid:guid}")]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid employeeGuid, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(new GetEmployeeByIdQuery(employeeGuid), cancellationToken));

    /// <summary>Create an employee. multipart/form-data so an image can be attached.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromForm] CreateEmployeeCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(command, cancellationToken));

    /// <summary>Replace an employee's details. Optionally upload a new Image, or set RemoveImage=true.</summary>
    [HttpPut("{employeeGuid:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> Update(Guid employeeGuid, [FromForm] UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        command.EmployeeGuid = employeeGuid;
        return ToActionResult(await mediator.Send(command, cancellationToken));
    }

    /// <summary>Upload or replace ONLY the employee's image (multipart field "Image").</summary>
    [HttpPut("{employeeGuid:guid}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> UploadImage(Guid employeeGuid, [FromForm] UploadEmployeeImageCommand command, CancellationToken cancellationToken)
    {
        command.EmployeeGuid = employeeGuid;
        return ToActionResult(await mediator.Send(command, cancellationToken));
    }

    /// <summary>Remove ONLY the employee's image.</summary>
    [HttpDelete("{employeeGuid:guid}/image")]
    public async Task<IActionResult> RemoveImage(Guid employeeGuid, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(new RemoveEmployeeImageCommand(employeeGuid), cancellationToken));

    /// <summary>Soft-delete an employee and remove their image.</summary>
    [HttpDelete("{employeeGuid:guid}")]
    public async Task<IActionResult> Delete(Guid employeeGuid, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(new DeleteEmployeeCommand(employeeGuid), cancellationToken));
}
