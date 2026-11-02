namespace Warehouse.Domain.Entities;

// A person who works at the hub. Can exist as a record only. UserId is set when the person is given system access.
// Status and role are NOT here: they belong to the login account (ApplicationUser).
public class Employee
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateOnly? StartDate { get; private set; }

    // Small resized image as a data URL (data:image/jpeg;base64,...). Optional.
    public string? ProfilePhoto { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Employee Create(string name, string email, string? phone, DateOnly? startDate, string? profilePhoto, DateTime nowUtc) => new()
    {
        Name = name,
        Email = email,
        Phone = phone,
        StartDate = startDate,
        ProfilePhoto = profilePhoto,
        CreatedAtUtc = nowUtc,
        UpdatedAtUtc = nowUtc
    };

    public void SetProfilePhoto(string? profilePhoto, DateTime nowUtc)
    {
        ProfilePhoto = profilePhoto;
        UpdatedAtUtc = nowUtc;
    }

    public void Update(string name, string email, string? phone, DateOnly? startDate, string? profilePhoto, DateTime nowUtc)
    {
        Name = name;
        Email = email;
        Phone = phone;
        StartDate = startDate;
        ProfilePhoto = profilePhoto;
        UpdatedAtUtc = nowUtc;
    }
}
