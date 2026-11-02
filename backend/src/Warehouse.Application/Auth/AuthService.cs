using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Warehouse.Application.Abstractions;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    // Used to spend the same hashing time when the email is unknown (stops user enumeration by timing).
    private static readonly PasswordHasher<ApplicationUser> TimingHasher =
        new(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = AuthConstants.PasswordHashIterations }));

    private static readonly Lazy<string> TimingHash = new(() =>
        TimingHasher.HashPassword(null!, "Timing#Dummy-Password-0"));

    public async Task<Result<AuthTokens>> LoginAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email.Trim());
        if (user is null)
        {
            TimingHasher.VerifyHashedPassword(null!, TimingHash.Value, command.Password);
            return Result<AuthTokens>.Unauthorized();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Login blocked, account locked. User {UserId}", user.Id);
            return Result<AuthTokens>.Unauthorized();
        }

        if (!await userManager.CheckPasswordAsync(user, command.Password))
        {
            await userManager.AccessFailedAsync(user);
            logger.LogWarning("Failed login. User {UserId}", user.Id);
            return Result<AuthTokens>.Unauthorized();
        }

        if (user.Status != UserStatus.Active)
            return Result<AuthTokens>.Unauthorized();

        await userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAtUtc = Now();
        user.UpdatedAtUtc = user.LastLoginAtUtc.Value;

        await userManager.UpdateAsync(user);
        return Result<AuthTokens>.Success(await IssueTokensAsync(user, null, command.RememberMe, ct));
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string? rawRefreshToken,CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return Result<AuthTokens>.Unauthorized();

        var now = Now();
        var stored = await refreshTokens.GetByHashAsync(tokenService.HashRefreshToken(rawRefreshToken), ct);
        if (stored is null) return Result<AuthTokens>.Unauthorized();

        if (stored.IsRevoked)
        {
            // A used token came back: possible theft. End every session of this user.
            logger.LogWarning("Refresh token reuse detected. User {UserId}", stored.UserId);
            await refreshTokens.RevokeAllForUserAsync(stored.UserId, now, ct);
            return Result<AuthTokens>.Unauthorized();
        }

        if (stored.IsExpired(now)) return Result<AuthTokens>.Unauthorized();

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || user.Status != UserStatus.Active || await userManager.IsLockedOutAsync(user))
            return Result<AuthTokens>.Unauthorized();

        return Result<AuthTokens>.Success(await IssueTokensAsync(user, stored, stored.IsPersistent, ct));
    }

    public async Task LogoutAsync(string? rawRefreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken)) return;

        var now = Now();
        var stored = await refreshTokens.GetByHashAsync(tokenService.HashRefreshToken(rawRefreshToken), ct);
        if (stored is null || !stored.IsActive(now)) return;

        stored.Revoke(now);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Sliding session: every refresh issues a new token with a fresh lifetime and keeps the same persistent choice.
    // So an active user stays signed in until they sign out, and is only signed out after being away for the full lifetime.
    private async Task<AuthTokens> IssueTokensAsync(ApplicationUser user, RefreshToken? replacing, bool persistent, CancellationToken ct)
    {
        var now = Now();
        var access = tokenService.CreateAccessToken(user);

        var rawRefresh = tokenService.GenerateRefreshToken();
        var refreshHash = tokenService.HashRefreshToken(rawRefresh);
        var refreshExpires = now.Add(tokenService.GetRefreshTokenLifetime(persistent));

        // Rotation: the old token is revoked and the new one saved in the same SaveChanges.
        replacing?.Revoke(now, refreshHash);
        refreshTokens.Add(RefreshToken.Create(user.Id, refreshHash, now, refreshExpires, persistent));
        await unitOfWork.SaveChangesAsync(ct);

        var dto = AuthUserDto.From(user);
        return new AuthTokens(access.Token, access.ExpiresAtUtc, rawRefresh, refreshExpires, persistent, dto);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
