using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Warehouse.Application.Employees;
using Warehouse.Application.Profile;
using Warehouse.Domain.Enums;

namespace Warehouse.Api.Contracts;

// Australian phone: mobile 04xx xxx xxx or +61 4xx xxx xxx (spaces optional).
[AttributeUsage(AttributeTargets.Property)]
public sealed partial class AustralianPhoneAttribute : ValidationAttribute
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^(?:\+61\s?4|04)\d{2}\s?\d{3}\s?\d{3}$")]
    private static partial System.Text.RegularExpressions.Regex Pattern();

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var phone = (value as string)?.Trim();
        if (string.IsNullOrEmpty(phone) || Pattern().IsMatch(phone)) return ValidationResult.Success;
        return new ValidationResult("Enter an Australian mobile number, for example 0412 345 678 or +61 412 345 678.");
    }
}

// Photo is a small image sent as a data URL. The browser resizes it first, this is the server-side limit.
[AttributeUsage(AttributeTargets.Property)]
public sealed class PhotoDataUrlAttribute : ValidationAttribute
{
    public const int MaxLength = 200_000;

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var photo = value as string;
        if (string.IsNullOrEmpty(photo)) return ValidationResult.Success;

        var ok = photo.Length <= MaxLength
                 && (photo.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal)
                     || photo.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
                     || photo.StartsWith("data:image/webp;base64,", StringComparison.Ordinal));

        return ok ? ValidationResult.Success : new ValidationResult("Photo must be a JPEG, PNG or WebP image under 150 KB.");
    }
}

// Null or empty removes the photo.
public sealed class SetPhotoRequest
{
    [PhotoDataUrl]
    public string? ProfilePhoto { get; init; }
}

// Own profile: no role, status or password here.
public sealed class UpdateProfileRequest
{
    [TrimmedText(2, 100)]
    public string Name { get; init; } = string.Empty;

    [Required, StrictEmail]
    public string Email { get; init; } = string.Empty;

    [AustralianPhone]
    public string? Phone { get; init; }

    public UpdateProfileCommand ToCommand() => new(Name, Email, Phone);
}

public sealed class ChangePasswordRequest
{
    [Required, StringLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class SaveEmployeeRequest
{
    [TrimmedText(2, 100)]
    public string Name { get; init; } = string.Empty;

    [Required, StrictEmail]
    public string Email { get; init; } = string.Empty;

    [AustralianPhone]
    public string? Phone { get; init; }

    public DateOnly? StartDate { get; init; }

    [PhotoDataUrl]
    public string? ProfilePhoto { get; init; }

    // false = employee record only, no login. true = the employee can log in with Email + Password.
    public bool HasAccess { get; init; }

    // Role, Status and Password are used only when HasAccess is true.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EmployeeRole? Role { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserStatus? Status { get; init; }

    // Required when the login is first created. Later, leave empty to keep the current password.
    [StringLength(128)]
    public string? Password { get; init; }

    public SaveEmployeeCommand ToCommand() => new(Name, Email, Phone, StartDate, ProfilePhoto, HasAccess, Role, Status, Password);
}
