using System.ComponentModel.DataAnnotations;

namespace Intellishelf.Api.Contracts.Auth;

public record LoginRequestContract([EmailAddress] string Email, string Password);