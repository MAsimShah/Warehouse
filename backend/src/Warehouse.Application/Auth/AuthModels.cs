using Warehouse.Domain.Entities;

namespace Warehouse.Application.Auth;

public static class AuthConstants
{
    public const int PasswordHashIterations = 600_000;
    public const int MaxFailedAccessAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}

// Commands arrive already validated by the API layer.
public sealed record LoginCommand(string Email, string Password, bool RememberMe);

public sealed record AuthUserDto(Guid Id, string Name, string Email, bool IsSuperAdmin, string Role, string? ProfilePhoto)
{
    public static AuthUserDto From(ApplicationUser user) => new(
        user.Id, user.Name, user.Email ?? string.Empty, user.IsSuperAdmin, user.Role.ToString(), user.ProfilePhoto);
}

// RefreshToken is the raw value. The API puts it in an HttpOnly cookie and never in the response body.
public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    bool RefreshTokenIsPersistent,
    AuthUserDto User);

public enum ResultStatus
{
    Success,
    ValidationFailed,
    Conflict,
    Unauthorized,
    NotFound
}

public sealed class Result<T>
{
    public ResultStatus Status { get; }
    public T? Value { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    private Result(ResultStatus status, T? value, IReadOnlyDictionary<string, string[]>? errors)
    {
        Status = status;
        Value = value;
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, null);
    public static Result<T> Invalid(IReadOnlyDictionary<string, string[]> errors) => new(ResultStatus.ValidationFailed, default, errors);
    public static Result<T> Conflict() => new(ResultStatus.Conflict, default, null);
    public static Result<T> Unauthorized() => new(ResultStatus.Unauthorized, default, null);
    public static Result<T> NotFound() => new(ResultStatus.NotFound, default, null);
}
