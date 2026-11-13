using System.Diagnostics;
using FluentValidation;
using LearningMap.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LearningMap.Api.Filters;

/// <summary>
/// Catches MODEL BINDING errors, e.g. DateOfJoining = "abc" or a malformed JSON body,
/// before the action runs. (Business rules are checked later by FluentValidation.)
/// </summary>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid) return;

        var errors = context.ModelState
            .Where(ms => ms.Value?.Errors.Count > 0)
            .Select(ms => new { PropertyName = ms.Key, ErrorMessage = string.Join(" ", ms.Value!.Errors.Select(e => e.ErrorMessage)) })
            .ToArray();

        context.Result = new BadRequestObjectResult(ApiResponseData.Fail(
            StatusCodes.Status400BadRequest,
            string.Join("; ", errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")),
            errors));
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

/// <summary>
/// One place that turns exceptions into responses, so controllers need no try/catch
/// (SafeSpace controllers wrap every action in try/catch; this filter replaces that).
/// </summary>
public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        if (context.Exception is ValidationException validation)
        {
            var errors = validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToArray();
            context.Result = new BadRequestObjectResult(ApiResponseData.Fail(
                StatusCodes.Status400BadRequest,
                string.Join("; ", errors.Select(e => e.ErrorMessage)),
                errors));
        }
        else
        {
            // Log the full details server-side, but never send stack traces to the client.
            logger.LogError(context.Exception, "Unhandled exception. TraceId {TraceId}", traceId);
            context.Result = new ObjectResult(ApiResponseData.Fail(
                StatusCodes.Status500InternalServerError,
                $"An unexpected error occurred. TraceId: {traceId}"))
            {
                StatusCode = StatusCodes.Status500InternalServerError,
            };
        }

        context.ExceptionHandled = true;
    }
}
