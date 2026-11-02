namespace Warehouse.Application.Options;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // Must come from user-secrets or an environment variable (Jwt__SigningKey). Never commit it.
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;
    // Normal sign-in: session cookie (ends when the browser closes), server side valid for this many days.
    public int RefreshTokenDays { get; set; } = 1;

    // "Keep me signed in": persistent cookie. Each refresh extends it, so it only ends after this many idle days.
    public int RememberMeRefreshTokenDays { get; set; } = 30;
}
