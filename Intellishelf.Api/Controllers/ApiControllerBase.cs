using System.Security.Claims;
using Intellishelf.Common.TryResult;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    protected abstract int MapErrorToStatusCode(string code);

    protected ObjectResult HandleErrorResponse(Error error)
    {
        var statusCode = MapErrorToStatusCode(error.Code);

        var problem = new ProblemDetails
        {
            Detail = error.Message,
            Status = statusCode,
            Extensions =
            {
                ["code"] = error.Code
            }
        };

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}