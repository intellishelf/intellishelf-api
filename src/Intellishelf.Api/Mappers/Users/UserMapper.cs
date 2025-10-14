using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Domain.Users.Models;

namespace Intellishelf.Api.Mappers.Users;

public class UserMapper : IUserMapper
{
    public LoginRequest MapLoginRequest(LoginRequestContract contract) =>
        new(contract.Email, contract.Password);

    public LoginResultContract MapLoginResult(LoginResult model) =>
        new(model.AccessToken, model.RefreshToken, model.AccessTokenExpiry, model.RefreshTokenExpiry);

    public UserResponseContract MapUser(User contract) =>
        new(contract.Id, contract.Email);
        
    public RefreshTokenRequest MapRefreshTokenRequest(RefreshTokenRequestContract contract) =>
        new(contract.RefreshToken);
}