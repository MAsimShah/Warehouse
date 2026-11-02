using Microsoft.AspNetCore.Identity;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Identity;

// Extra password rules on top of the Identity length and character rules.
internal sealed class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly string[] BannedFragments =
    [
        "password", "passw0rd", "qwerty", "letmein", "welcome", "admin", "iloveyou",
        "123456", "abc123", "warehouse", "changeme", "monkey", "dragon"
    ];

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (string.IsNullOrEmpty(password)) return Task.FromResult(IdentityResult.Success);

        var lower = password.ToLowerInvariant();

        if (BannedFragments.Any(lower.Contains))
            return Fail("PasswordTooCommon", "Password is too common or easy to guess.");

        var localPart = user.Email?.Split('@')[0].ToLowerInvariant();
        if (!string.IsNullOrEmpty(localPart) && localPart.Length >= 4 && lower.Contains(localPart))
            return Fail("PasswordContainsEmail", "Password must not contain your email name.");

        return Task.FromResult(IdentityResult.Success);
    }

    private static Task<IdentityResult> Fail(string code, string description) =>
        Task.FromResult(IdentityResult.Failed(new IdentityError { Code = code, Description = description }));
}
