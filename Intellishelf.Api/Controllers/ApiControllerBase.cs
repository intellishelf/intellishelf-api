using System.Security.Claims;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Ai.Errors;
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
        // 404 Not Found
        BookErrorCodes.BookNotFound => StatusCodes.Status404NotFound,
        
        // 401 Unauthorized
        UserErrorCodes.UserNotFound or
        BookErrorCodes.AccessDenied or 
        UserErrorCodes.Unauthorized or 
        UserErrorCodes.RefreshTokenExpired or
        UserErrorCodes.RefreshTokenNotFound  or
        UserErrorCodes.RefreshTokenRevoked or
        UserErrorCodes.OAuthError => StatusCodes.Status401Unauthorized,
        
        // 409 Conflict
        UserErrorCodes.AlreadyExists => StatusCodes.Status409Conflict,
        
        // 500 Internal Server Error
        FileErrorCodes.UploadFailed or 
        FileErrorCodes.DeletionFailed or 
        AiErrorCodes.AiResponseNotParsed or 
        AiErrorCodes.RequestFailed => StatusCodes.Status500InternalServerError,
        
        _ => StatusCodes.Status500InternalServerError
    };

    protected static ObjectResult HandleErrorResponse(Error error)
    {
        var statusCode = MapErrorToStatusCode(error.Code);

        var problem = new ProblemDetails
        {
            Title = error.Message,
            Status = statusCode,
            Type = error.Code
        };

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }
}