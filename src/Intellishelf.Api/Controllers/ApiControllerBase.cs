using System.Security.Claims;
using Intellishelf.Common.TryResult;
using Intellishelf.Domain.Ai.Errors;
using Intellishelf.Domain.Books.Errors;
using Intellishelf.Domain.Chat.Errors;
using Intellishelf.Domain.Files.ErrorCodes;
using Intellishelf.Domain.Users.Models;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value!;

    private static int MapErrorToStatusCode(string code) => code switch
    {
        // 404 Not Found
        BookErrorCodes.BookNotFound or
        BookErrorCodes.IsbnNotFound => StatusCodes.Status404NotFound,

        // 401 Unauthorized
        UserErrorCodes.UserNotFound or
        UserErrorCodes.Unauthorized or
        UserErrorCodes.RefreshTokenExpired or
        UserErrorCodes.RefreshTokenNotFound  or
        UserErrorCodes.RefreshTokenRevoked or
        UserErrorCodes.OAuthError => StatusCodes.Status401Unauthorized,

        // 409 Conflict
        UserErrorCodes.AlreadyExists or
        BookErrorCodes.DuplicateIsbn => StatusCodes.Status409Conflict,

        // 400 Bad Request
        FileErrorCodes.InvalidFileType or
        FileErrorCodes.FileTooLarge or
        BookErrorCodes.InvalidIsbn or
        ChatErrorCodes.EmptyConversation or
        ChatErrorCodes.InvalidMessage => StatusCodes.Status400BadRequest,

        // 502 Bad Gateway (external service errors)
        BookErrorCodes.MetadataServiceError or
        BookErrorCodes.CoverImageDownloadFailed => StatusCodes.Status502BadGateway,

        // 500 Internal Server Error
        FileErrorCodes.UploadFailed or
        FileErrorCodes.DeletionFailed or
        AiErrorCodes.AiResponseNotParsed or
        AiErrorCodes.RequestFailed or
        ChatErrorCodes.AiRequestFailed => StatusCodes.Status500InternalServerError,

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