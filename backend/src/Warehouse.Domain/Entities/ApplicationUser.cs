using Microsoft.AspNetCore.Identity;
using Warehouse.Domain.Enums;

namespace Warehouse.Domain.Entities;

// Login account only (email, password hash, role, status). Personal details live in Employee.
public class ApplicationUser : IdentityUser<Guid>
{
    // Copy of the employee name, kept in sync by the employee service. Used in the token.
    public string Name { get; private set; } = string.Empty;

    // Set only by the signup flow. A Super Admin owns the whole system, not part of any organisation.
    public bool IsSuperAdmin { get; private set; }

    public EmployeeRole Role { get; private set; } = EmployeeRole.SuperAdmin;

    // Copy of Employee.ProfilePhoto (a Super Admin has no employee record, so the photo must live here too).
    // Small resized image as a data URL. Always change it through IProfileService / EmployeeService so both stay in step.
    public string? ProfilePhoto { get; private set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    public static ApplicationUser CreateSuperAdmin(string name, string email, DateTime nowUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Email = email,
        UserName = email,
        IsSuperAdmin = true,
        Role = EmployeeRole.SuperAdmin,
        CreatedAtUtc = nowUtc,
        UpdatedAtUtc = nowUtc
    };

    public static ApplicationUser CreateLogin(
        string name, string email, EmployeeRole role, UserStatus status, string? profilePhoto, DateTime nowUtc) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Email = email,
        UserName = email,
        Role = role,
        ProfilePhoto = profilePhoto,
        Status = status,
        IsSuperAdmin = false,
        CreatedAtUtc = nowUtc,
        UpdatedAtUtc = nowUtc
    };

    public void UpdateLogin(
        string name, string email, EmployeeRole role, UserStatus status, string? profilePhoto, DateTime nowUtc)
    {
        Name = name;
        Email = email;
        UserName = email;
        Role = role;
        Status = status;
        ProfilePhoto = profilePhoto;
        UpdatedAtUtc = nowUtc;
    }

    public void SetProfilePhoto(string? profilePhoto, DateTime nowUtc)
    {
        ProfilePhoto = profilePhoto;
        UpdatedAtUtc = nowUtc;
    }
}
