using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Persistence;

internal sealed class EmployeeRepository(WarehouseDbContext db) : IEmployeeRepository
{
    public async Task<IReadOnlyList<Employee>> ListAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Employees.AsNoTracking().Include(e => e.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.Name.Contains(term) || e.Email.Contains(term));
        }

        return await query.OrderBy(e => e.Name).ToListAsync(ct);
    }

    public Task<Employee?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Employees.Include(e => e.User).FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Employees.FirstOrDefaultAsync(e => e.UserId == userId, ct);

    public Task<bool> EmailExistsAsync(string email, Guid? exceptEmployeeId, CancellationToken ct = default) =>
        db.Employees.AnyAsync(e => e.Email == email && e.Id != exceptEmployeeId, ct);

    public void Add(Employee employee) => db.Employees.Add(employee);

    public void Remove(Employee employee) => db.Employees.Remove(employee);
}

internal sealed class RefreshTokenRepository(WarehouseDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);

    public Task RevokeAllForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct = default) =>
        db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, nowUtc), ct);
}
