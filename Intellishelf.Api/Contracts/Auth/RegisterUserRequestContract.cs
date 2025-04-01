using System.ComponentModel.DataAnnotations;

namespace Intellishelf.Api.Contracts.Auth;

public record RegisterUserRequestContract([EmailAddress] string Email, string Password);