using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Domain.Auth.Models;

namespace Intellishelf.Api.Mappers.Auth;

public class AuthMapper : IAuthMapper
{
    public Login Map(LoginRequestContract contract) =>
        new(contract.UserName, contract.Password);
}