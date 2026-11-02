using Warehouse.Domain.Enums;

namespace Warehouse.Application.Employees;

// Commands arrive already validated by the API layer.
// HasAccess = false: record only (no login). HasAccess = true: a login account is created or kept, and needs Role.
// Password is required when the login is created, optional afterwards (reset).
public sealed record SaveEmployeeCommand(
    string Name,
    string Email,
    string? Phone,
    DateOnly? StartDate,
    string? ProfilePhoto,
    bool HasAccess,
    EmployeeRole? Role,
    UserStatus? Status,
    string? Password);

// Role and Status are null when the employee has no login.
public sealed record EmployeeDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    DateOnly? StartDate,
    string? ProfilePhoto,
    bool HasAccess,
    string? Role,
    string? Status,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
