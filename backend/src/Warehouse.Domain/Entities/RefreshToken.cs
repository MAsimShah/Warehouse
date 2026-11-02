namespace Warehouse.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid UserId { get; private set; }

    // Only the SHA-256 hash is stored. The raw token lives only in the client cookie.
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    // True when the user chose "Keep me signed in". The cookie then survives closing the browser.
    public bool IsPersistent { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public ApplicationUser User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime nowUtc, DateTime expiresAtUtc, bool isPersistent) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        CreatedAtUtc = nowUtc,
        ExpiresAtUtc = expiresAtUtc,
        IsPersistent = isPersistent
    };

    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsExpired(DateTime nowUtc) => ExpiresAtUtc <= nowUtc;
    public bool IsActive(DateTime nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    public void Revoke(DateTime nowUtc, string? replacedByTokenHash = null)
    {
        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
