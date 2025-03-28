using Intellishelf.Api.Contracts.Auth;
using Intellishelf.Domain.Auth.Models;

namespace Intellishelf.Api.Mappers.Auth;

public interface IAuthMapper
{
    LoginRequest Map(LoginRequestContract contract);
}