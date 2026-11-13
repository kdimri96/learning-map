using LearningMap.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace LearningMap.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Sends ApiResponseData with a MATCHING HTTP status code.
    /// (SafeSpace returns ApiResponseData directly, so the HTTP status is always 200 and the real
    /// status is only inside the body. Using the real status lets clients, proxies and logs see errors.)
    /// </summary>
    protected IActionResult ToActionResult(ApiResponseData response) => StatusCode(response.StatusCode, response);
}
