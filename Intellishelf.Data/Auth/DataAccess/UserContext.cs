using Intellishelf.Domain.Auth.DataAccess;
using Microsoft.AspNetCore.Http;

namespace Intellishelf.Data.Auth.DataAccess;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string GetCurrentUserId() =>
        httpContextAccessor.HttpContext.User.FindFirst("userId").Value;
}