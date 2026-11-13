using System.Security.Claims;
using LearningMap.Application.Interfaces.Services;
using LearningMap.Domain.Common;

namespace LearningMap.Api.Services;

/// <summary>Reads the logged-in user from the JWT claims of the current HTTP request.</summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public int? UserId => int.TryParse(User?.FindFirstValue(AppClaimTypes.UserId), out var id) ? id : null;

    public string? Email => User?.FindFirstValue(AppClaimTypes.Email);
}
