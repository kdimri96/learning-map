using LearningMap.Api.Extensions;
using LearningMap.Application.Commands.Auth;
using LearningMap.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LearningMap.Api.Controllers;

/// <summary>
/// Controllers stay THIN: receive the request, hand it to MediatR, return the result.
/// All logic lives in the handlers (Application layer).
/// </summary>
[Route("auth")]
public class AuthController(ISender mediator) : ApiControllerBase
{
    /// <summary>Log in with email + password. Returns a JWT to send as "Authorization: Bearer {token}".</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ServiceExtensions.LoginRateLimitPolicy)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(command, cancellationToken));

    /// <summary>Create an account. Any logged-in account can manage employees.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponseData), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command, CancellationToken cancellationToken) =>
        ToActionResult(await mediator.Send(command, cancellationToken));

    /// <summary>Shows what the API reads from your token. Handy for learning what a JWT contains.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me() => ToActionResult(ApiResponseData.Success(new
    {
        UserId = User.FindFirst(AppClaimTypes.UserId)?.Value,
        Email = User.FindFirst(AppClaimTypes.Email)?.Value,
        Name = User.Identity?.Name,
        Role = User.FindFirst(AppClaimTypes.Role)?.Value,
        Claims = User.Claims.Select(c => new { c.Type, c.Value }),
    }));
}
