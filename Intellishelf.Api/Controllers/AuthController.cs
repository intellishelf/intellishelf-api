using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Api.Mappers.Users;
using Intellishelf.Domain.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("auth")]
public class AuthController(IUserMapper mapper, IAuthService authService) : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> Login([FromBody] LoginRequestContract loginRequest)
    {
        var result = await authService.TrySignInAsync(mapper.MapLoginRequest(loginRequest));

        return !result.IsSuccess
            ? HandleErrorResponse(result.Error)
            : Ok(mapper.MapLoginResult(result.Value));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult> Register([FromBody] RegisterUserRequestContract registerRequest)
    {
        var result = await authService.TryRegisterAsync(mapper.MapRegisterUserRequest(registerRequest));

        return !result.IsSuccess
            ? HandleErrorResponse(result.Error)
            : Ok(mapper.MapLoginResult(result.Value));
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponseContract>> Me()
    {
        var result = await authService.TryFindByIdAsync(CurrentUserId);

        return result.IsSuccess
            ? Ok(mapper.MapUser(result.Value))
            : HandleErrorResponse(result.Error);
    }
}