using System.Security.Claims;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Files.ErrorCodes;
using Intellishelf.Domain.Users.ErrorCodes;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    private static int MapErrorToStatusCode(string code) => code switch
    {
        BookErrorCodes.BookNotFound or UserErrorCodes.UserNotFound or FileErrorCodes.DownloadingFailed => StatusCodes.Status404NotFound,
        UserErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
        UserErrorCodes.AlreadyExists => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    protected ObjectResult HandleErrorResponse(Error error)
    {
        var statusCode = MapErrorToStatusCode(error.Code);

        var problem = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = statusCode
        };

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}