using System.ComponentModel.DataAnnotations;

namespace Intellishelf.Api.Contracts.Auth;

public record RegisterUserRequestContract(
    [EmailAddress] string Email, 
    [MinLength(6)] string Password);
