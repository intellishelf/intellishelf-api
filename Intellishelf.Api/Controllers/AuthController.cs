using System.Security.Claims;
using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Api.Mappers.Users;
using Intellishelf.Domain.Users.Config;
using Intellishelf.Domain.Users.Models;
using Intellishelf.Domain.Users.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace Intellishelf.Api.Controllers;

[ApiController]
[Authorize]
[Route("auth")]
public class AuthController(
    IUserMapper mapper,
    IAuthService authService,
    IOptions<AuthConfig> authOptions) : ApiControllerBase
{
    private readonly AuthConfig _authConfig = authOptions.Value;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> Register([FromBody] RegisterUserRequestContract registerRequest)
    {
        var result = await authService.TryRegisterAsync(registerRequest.Email, registerRequest.Password);

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(mapper.MapLoginResult(result.Value));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> Login([FromBody] LoginRequestContract loginRequest)
    {
        var result = await authService.TrySignInAsync(mapper.MapLoginRequest(loginRequest));

        if (!result.IsSuccess)
            return HandleErrorResponse(result.Error);

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(mapper.MapLoginResult(result.Value));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultContract>> RefreshToken([FromBody] RefreshTokenRequestContract? request)
    {
        var refreshToken = Request.Cookies[_authConfig.RefreshTokenCookieName]
                           ?? request?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Unauthorized();

        var result = await authService.TryRefreshTokenAsync(new RefreshTokenRequest(refreshToken));

        if (!result.IsSuccess)
        {
            ClearRefreshCookie();
            return HandleErrorResponse(result.Error);
        }

        SetRefreshCookie(result.Value.RefreshToken, result.Value.RefreshTokenExpiry);

        return Ok(mapper.MapLoginResult(result.Value));
    }

    [HttpPost("revoke")]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequestContract request)
    {
        var result = await authService.TryRevokeRefreshTokenAsync(mapper.MapRefreshTokenRequest(request));

        return !result.IsSuccess
            ? HandleErrorResponse(result.Error)
            : Ok();
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = Request.Cookies[_authConfig.RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ClearRefreshCookie();
            return NoContent();
        }

        var result = await authService.TryRevokeRefreshTokenAsync(new RefreshTokenRequest(refreshToken));

        ClearRefreshCookie();

        return result.IsSuccess
            ? NoContent()
            : HandleErrorResponse(result.Error);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponseContract>> Me()
    {
        var result = await authService.TryFindByIdAsync(CurrentUserId);

        return result.IsSuccess
            ? Ok(mapper.MapUser(result.Value))
            : HandleErrorResponse(result.Error);
    }

    [HttpGet("google")]
    [AllowAnonymous]
    public IActionResult SignInWithGoogle([FromQuery] string? returnUrl = null)
    {
        var redirectUri = Url.Action(nameof(GoogleLoginCallback), new { returnUrl });

        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUri
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLoginCallback([FromQuery] string? returnUrl = null)
    {
        var auth = await HttpContext.AuthenticateAsync(AuthConfig.CookieScheme);

        if (!auth.Succeeded || auth.Principal is null)
        {
            return Unauthorized();
        }

        var email = auth.Principal.FindFirstValue(ClaimTypes.Email);
        var externalId = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(externalId))
        {
            return Unauthorized();
        }

        await HttpContext.SignOutAsync(AuthConfig.CookieScheme);

        var signInResult = await authService.TrySignInExternalAsync(
            new ExternalLoginRequest(email, externalId, AuthProvider.Google));

        if (!signInResult.IsSuccess)
        {
            return HandleErrorResponse(signInResult.Error);
        }

        await ReplaceUserIdentifier(auth.Principal, signInResult.Value.UserId);

        SetRefreshCookie(signInResult.Value.RefreshToken, signInResult.Value.RefreshTokenExpiry);

        return Redirect(!string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/");
    }

    private async Task ReplaceUserIdentifier(ClaimsPrincipal principal, string userId)
    {
        var claims = principal.Claims.ToList();

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim != null) claims.Remove(userIdClaim);

        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        await HttpContext.SignInAsync(
            AuthConfig.CookieScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, AuthConfig.CookieScheme))
          );
    }

    private void SetRefreshCookie(string refreshToken, DateTime expiry) => SetCookie(refreshToken, expiry);

    private void ClearRefreshCookie() => SetCookie(string.Empty, DateTimeOffset.UnixEpoch);

    private void SetCookie(string value, DateTimeOffset expiry)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiry,
            IsEssential = true
        };

        Response.Cookies.Append(_authConfig.RefreshTokenCookieName, value, options);
    }




}