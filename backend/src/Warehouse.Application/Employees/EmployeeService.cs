using Microsoft.AspNetCore.Identity;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Auth;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Employees;

public sealed class EmployeeService(
    UserManager<ApplicationUser> userManager,
    IEmployeeRepository employees,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IEmployeeService
{
    public async Task<IReadOnlyList<EmployeeDto>> ListAsync(string? search, CancellationToken ct = default) =>
        (await employees.ListAsync(search, ct)).Select(ToDto).ToList();

    public async Task<Result<EmployeeDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetAsync(id, ct);
        return employee is null ? Result<EmployeeDto>.NotFound() : Result<EmployeeDto>.Success(ToDto(employee));
    }

    public async Task<Result<EmployeeDto>> CreateAsync(SaveEmployeeCommand c, CancellationToken ct = default)
    {
        var email = c.Email.Trim();
        var now = Now();

        var problem = await CheckAsync(c, existingUser: null, existingEmployeeId: null, email, ct);
        if (problem is not null) return problem;

        var employee = Employee.Create(c.Name.Trim(), email, NullIfEmpty(c.Phone), c.StartDate, NullIfEmpty(c.ProfilePhoto), now);

        ApplicationUser? user = null;
        if (c.HasAccess)
        {
            user = ApplicationUser.CreateLogin(
                employee.Name, email, c.Role!.Value, c.Status ?? UserStatus.Active, employee.ProfilePhoto, now);
            var created = await userManager.CreateAsync(user, c.Password!);
            if (!created.Succeeded) return Result<EmployeeDto>.Invalid(ToErrors(created));
            employee.UserId = user.Id;
            employee.User = user;
        }

        employees.Add(employee);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            // Do not leave a login without an employee record.
            if (user is not null) await userManager.DeleteAsync(user);
            return Invalid("email", "An employee with this email already exists.");
        }

        return Result<EmployeeDto>.Success(ToDto(employee));
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(Guid id, SaveEmployeeCommand c, CancellationToken ct = default)
    {
        var employee = await employees.GetAsync(id, ct);
        if (employee is null) return Result<EmployeeDto>.NotFound();

        var email = c.Email.Trim();
        var now = Now();
        var user = employee.User;

        var problem = await CheckAsync(c, user, employee.Id, email, ct);
        if (problem is not null) return problem;

        employee.Update(c.Name.Trim(), email, NullIfEmpty(c.Phone), c.StartDate, NullIfEmpty(c.ProfilePhoto), now);

        var revokeSessions = false;

        if (user is not null)
        {
            // Email, name, role and status stay in step with the employee. Turning access off makes the login Inactive.
            var status = c.HasAccess ? c.Status ?? UserStatus.Active : UserStatus.Inactive;
            var role = c.HasAccess ? c.Role!.Value : user.Role;
            revokeSessions = status == UserStatus.Inactive && user.Status == UserStatus.Active;

            user.UpdateLogin(employee.Name, email, role, status, employee.ProfilePhoto, now);
            var updated = await userManager.UpdateAsync(user);
            if (!updated.Succeeded) return Result<EmployeeDto>.Invalid(ToErrors(updated));

            if (c.HasAccess && !string.IsNullOrEmpty(c.Password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var reset = await userManager.ResetPasswordAsync(user, token, c.Password);
                if (!reset.Succeeded) return Result<EmployeeDto>.Invalid(ToErrors(reset));
                revokeSessions = true;
            }
        }
        else if (c.HasAccess)
        {
            user = ApplicationUser.CreateLogin(
                employee.Name, email, c.Role!.Value, c.Status ?? UserStatus.Active, employee.ProfilePhoto, now);
            var created = await userManager.CreateAsync(user, c.Password!);
            if (!created.Succeeded) return Result<EmployeeDto>.Invalid(ToErrors(created));
            employee.UserId = user.Id;
            employee.User = user;
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateKeyException)
        {
            return Invalid("email", "An employee with this email already exists.");
        }

        if (revokeSessions && user is not null) await refreshTokens.RevokeAllForUserAsync(user.Id, now, ct);

        return Result<EmployeeDto>.Success(ToDto(employee));
    }

    public async Task<Result<EmployeeDto>> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var employee = await employees.GetAsync(id, ct);
        if (employee is null) return Result<EmployeeDto>.NotFound();

        var user = employee.User;
        if (user is null)
        {
            // Record only, nothing depends on it: remove the record.
            var dto = ToDto(employee);
            employees.Remove(employee);
            await unitOfWork.SaveChangesAsync(ct);
            return Result<EmployeeDto>.Success(dto);
        }

        var now = Now();
        user.Status = UserStatus.Inactive;
        user.UpdatedAtUtc = now;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded) return Result<EmployeeDto>.Invalid(ToErrors(updated));

        await refreshTokens.RevokeAllForUserAsync(user.Id, now, ct);
        return Result<EmployeeDto>.Success(ToDto(employee));
    }

    // Shared checks for create and update. Returns a failed result, or null when everything is fine.
    private async Task<Result<EmployeeDto>?> CheckAsync(
        SaveEmployeeCommand c, ApplicationUser? existingUser, Guid? existingEmployeeId, string email, CancellationToken ct)
    {
        if (await employees.EmailExistsAsync(email, existingEmployeeId, ct))
            return Invalid("email", "An employee with this email already exists.");

        if (!c.HasAccess) return null;

        if (c.Role is null || !Enum.IsDefined(c.Role.Value) || c.Role == EmployeeRole.SuperAdmin)
            return Invalid("role", "Choose a valid role.");

        if (existingUser is null)
        {
            if (string.IsNullOrEmpty(c.Password)) return Invalid("password", "Password is required to give access.");
            if (await userManager.FindByEmailAsync(email) is not null)
                return Invalid("email", "A login with this email already exists.");
        }
        else
        {
            var other = await userManager.FindByEmailAsync(email);
            if (other is not null && other.Id != existingUser.Id)
                return Invalid("email", "A login with this email already exists.");
        }

        return null;
    }

    private static Result<EmployeeDto> Invalid(string field, string message) =>
        Result<EmployeeDto>.Invalid(new Dictionary<string, string[]> { [field] = [message] });

    private static Dictionary<string, string[]> ToErrors(IdentityResult result)
    {
        // Identity password errors have codes starting with "Password". Everything else goes under "email".
        var errors = result.Errors.ToLookup(e => e.Code.StartsWith("Password", StringComparison.Ordinal) ? "password" : "email");
        return errors.ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static EmployeeDto ToDto(Employee e) => new(
        e.Id, e.Name, e.Email, e.Phone, e.StartDate, e.ProfilePhoto,
        e.User is not null, e.User?.Role.ToString(), e.User?.Status.ToString(),
        e.CreatedAtUtc, e.User?.LastLoginAtUtc);
}
