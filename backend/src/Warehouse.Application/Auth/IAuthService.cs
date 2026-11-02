namespace Warehouse.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthTokens>> LoginAsync(LoginCommand command,CancellationToken ct = default);
    Task<Result<AuthTokens>> RefreshAsync(string? rawRefreshToken,CancellationToken ct = default);
    Task LogoutAsync(string? rawRefreshToken, CancellationToken ct = default);
}
