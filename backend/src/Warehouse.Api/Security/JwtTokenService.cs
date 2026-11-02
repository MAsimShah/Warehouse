using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Options;
using Warehouse.Domain.Entities;

namespace Warehouse.Api.Security;

public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider clock) : ITokenService
{
    public const string SuperAdminClaim = "is_super_admin";
    public const string RoleClaim = "employee_role";

    private readonly JwtSettings _settings = options.Value;

    public TimeSpan GetRefreshTokenLifetime(bool persistent) =>
        TimeSpan.FromDays(persistent ? _settings.RememberMeRefreshTokenDays : _settings.RefreshTokenDays);

    public AccessToken CreateAccessToken(ApplicationUser user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("name", user.Name),
            new(SuperAdminClaim, user.IsSuperAdmin ? "true" : "false"),
            new(RoleClaim, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public string GenerateRefreshToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
