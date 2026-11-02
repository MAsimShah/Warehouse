using System.ComponentModel.DataAnnotations;
using Warehouse.Application.Auth;

namespace Warehouse.Api.Contracts;

// Validated here by [ApiController] before the controller runs. The Application layer trusts its input.
public sealed class LoginRequest
{
    [Required, StrictEmail]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;

    // "Keep me signed in" checkbox.
    public bool RememberMe { get; init; }

    public LoginCommand ToCommand() => new(Email, Password, RememberMe);
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, AuthUserDto User);
