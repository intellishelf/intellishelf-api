using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Domain.Auth.Models;

namespace Intellishelf.Api.Mappers.Auth;

public class AuthMapper : IAuthMapper
{
    public LoginRequest Map(LoginRequestContract contract) =>
        new(contract.UserName, contract.Password);
}