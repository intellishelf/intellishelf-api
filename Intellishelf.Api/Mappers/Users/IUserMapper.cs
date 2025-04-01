using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Api.Mappers.Users;

public interface IUserMapper
{
    LoginRequest MapLoginRequest(LoginRequestContract contract);
    LoginResultContract MapLoginResult(LoginResult model);
    UserResponseContract MapUser(User contract);
    RegisterUserRequest MapRegisterUserRequest(RegisterUserRequestContract contract);
}