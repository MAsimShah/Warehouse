using Warehouse.Domain.Entities;

namespace Warehouse.Application.Abstractions;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    TimeSpan GetRefreshTokenLifetime(bool persistent);
    AccessToken CreateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    void Add(RefreshToken token);
    Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default);
}

public interface IEmployeeRepository
{
    // List is read-only (no tracking). The Get methods are tracked so changes are saved by IUnitOfWork.
    Task<IReadOnlyList<Employee>> ListAsync(string? search, CancellationToken ct = default);
    Task<Employee?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid? exceptEmployeeId, CancellationToken ct = default);
    void Add(Employee employee);
    void Remove(Employee employee);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// Thrown when a unique index is violated (for example two signups with the same email at once).
public sealed class DuplicateKeyException(Exception inner) : Exception("Duplicate key.", inner);
