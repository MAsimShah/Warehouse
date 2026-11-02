using Warehouse.Application.Auth;

namespace Warehouse.Application.Employees;

// Used by the Super Admin only (enforced by the API policy).
public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeDto>> ListAsync(string? search, CancellationToken ct = default);
    Task<Result<EmployeeDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<Result<EmployeeDto>> CreateAsync(SaveEmployeeCommand command, CancellationToken ct = default);
    Task<Result<EmployeeDto>> UpdateAsync(Guid id, SaveEmployeeCommand command, CancellationToken ct = default);

    // "Delete": a login is never hard-deleted (docs/requirements.md), it is set Inactive and its sessions end.
    // An employee with no login is only a record, so it is removed.
    Task<Result<EmployeeDto>> DeactivateAsync(Guid id, CancellationToken ct = default);
}
