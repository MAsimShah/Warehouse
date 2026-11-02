using Microsoft.AspNetCore.Identity;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Auth;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Profile;

// What the signed-in user sees and edits about themselves. Role and status are not here: only the Super Admin changes those.
// Phone and HasEmployeeRecord: a Super Admin has no employee record, so no phone.
public sealed record ProfileDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    bool HasEmployeeRecord,
    bool IsSuperAdmin,
    string Role,
    string? ProfilePhoto);

// Already validated by the API layer. Phone is ignored when the user has no employee record.
public sealed record UpdateProfileCommand(string Name, string Email, string? Phone);

// The signed-in user's own profile. The user id always comes from the token, never from the request.
public interface IProfileService
{
    Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileCommand command, CancellationToken ct = default);

    // photo = null removes the photo.
    Task<Result<ProfileDto>> SetPhotoAsync(Guid userId, string? photo, CancellationToken ct = default);

    // Needs the current password. All sessions are ended afterwards, the user signs in again with the new password.
    Task<Result<bool>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);
}

public sealed class ProfileService(
    UserManager<ApplicationUser> userManager,
    IEmployeeRepository employees,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IProfileService
{
    public async Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<ProfileDto>.NotFound();

        return Result<ProfileDto>.Success(ToDto(user, await employees.GetByUserIdAsync(userId, ct)));
    }

    // Name and email are kept the same in Users and Employees (one save for both).
    public async Task<Result<ProfileDto>> UpdateAsync(Guid userId, UpdateProfileCommand c, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<ProfileDto>.NotFound();

        var employee = await employees.GetByUserIdAsync(userId, ct);
        var name = c.Name.Trim();
        var email = c.Email.Trim();
        var now = Now();

        var other = await userManager.FindByEmailAsync(email);
        if ((other is not null && other.Id != user.Id) || await employees.EmailExistsAsync(email, employee?.Id, ct))
            return Invalid("email", "This email is already in use.");

        employee?.Update(
            name, email, string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone.Trim(), employee.StartDate, employee.ProfilePhoto, now);

        // Role, status and photo stay as they are.
        user.UpdateLogin(name, email, user.Role, user.Status, user.ProfilePhoto, now);

        // Saves the user and the (tracked) employee in one go.
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
            return Result<ProfileDto>.Invalid(new Dictionary<string, string[]> { ["email"] = updated.Errors.Select(e => e.Description).ToArray() });

        return Result<ProfileDto>.Success(ToDto(user, employee));
    }

    // The only place a user changes their own photo. It writes Users and, when the user is an employee,
    // Employees too, so the two copies never differ.
    public async Task<Result<ProfileDto>> SetPhotoAsync(Guid userId, string? photo, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<ProfileDto>.NotFound();

        var now = Now();
        var value = string.IsNullOrWhiteSpace(photo) ? null : photo;
        var employee = await employees.GetByUserIdAsync(userId, ct);

        user.SetProfilePhoto(value, now);
        employee?.SetProfilePhoto(value, now);

        await unitOfWork.SaveChangesAsync(ct);
        return Result<ProfileDto>.Success(ToDto(user, employee));
    }

    public async Task<Result<bool>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Result<bool>.NotFound();

        var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
        {
            var errors = changed.Errors.ToLookup(e => e.Code == "PasswordMismatch" ? "currentPassword" : "newPassword");
            return Result<bool>.Invalid(errors.ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray()));
        }

        await refreshTokens.RevokeAllForUserAsync(user.Id, Now(), ct);
        return Result<bool>.Success(true);
    }

    private static Result<ProfileDto> Invalid(string field, string message) =>
        Result<ProfileDto>.Invalid(new Dictionary<string, string[]> { [field] = [message] });

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;

    private static ProfileDto ToDto(ApplicationUser u, Employee? e) => new(
        u.Id, u.Name, u.Email ?? string.Empty, e?.Phone, e is not null, u.IsSuperAdmin, u.Role.ToString(), u.ProfilePhoto);
}
