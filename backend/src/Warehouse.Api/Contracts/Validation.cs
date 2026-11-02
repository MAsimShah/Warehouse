using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace Warehouse.Api.Contracts;

// Text of min..max characters after trimming, with no control characters.
[AttributeUsage(AttributeTargets.Property)]
public sealed class TrimmedTextAttribute(int min, int max) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var label = context.DisplayName;
        var text = (value as string)?.Trim() ?? string.Empty;

        if (text.Length < min || text.Length > max)
            return new ValidationResult($"{label} must be {min} to {max} characters.");
        if (text.Any(char.IsControl))
            return new ValidationResult($"{label} contains invalid characters.");

        return ValidationResult.Success;
    }
}

// Stricter than [EmailAddress]: the domain must contain a dot and the value must be a plain address.
[AttributeUsage(AttributeTargets.Property)]
public sealed class StrictEmailAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var email = (value as string)?.Trim();
        if (!string.IsNullOrEmpty(email)
            && email.Length <= 254
            && MailAddress.TryCreate(email, out var address)
            && address.Address == email
            && address.Host.Contains('.'))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Enter a valid email address.");
    }
}
