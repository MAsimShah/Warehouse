using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions;
using Warehouse.Application.Auth;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Infrastructure.Persistence;

public class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options), IUnitOfWork
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        builder.ApplyConfigurationsFromAssembly(typeof(WarehouseDbContext).Assembly);

        // Default Super Admin, inserted when the migration runs.
        // Initial password (development only, change it after first login): Qm7#vT2!xR9$kLp4Zb8&
        var seededAt = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc);
        builder.Entity<ApplicationUser>().HasData(new
        {
            Id = new Guid("0192a5c0-7e3b-7c10-9a4d-5b1f2c3d4e5f"),
            Name = "Shah corp",
            UserName = "shahcorp@gmail.com",
            NormalizedUserName = "SHAHCORP@GMAIL.COM",
            Email = "shahcorp@gmail.com",
            NormalizedEmail = "SHAHCORP@GMAIL.COM",
            EmailConfirmed = true,
            PasswordHash = HashSeedPassword("Qm7#vT2!xR9$kLp4Zb8&"),
            SecurityStamp = "7QJ2K5M4N6P8R3T9V1W2X4Y6Z8A3B5C7",
            ConcurrencyStamp = "b6f1c0de-4a2e-4f7b-9c3d-1e8a5d2f7a90",
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            IsSuperAdmin = true,
            Role = EmployeeRole.SuperAdmin,
            Status = UserStatus.Active,
            CreatedAtUtc = seededAt,
            UpdatedAtUtc = seededAt
        });
    }

    // Builds an Identity V3 hash (PBKDF2, HMAC-SHA512, 600000 iterations) with a fixed salt,
    // so the migration does not change every time it is generated.
    private static string HashSeedPassword(string password)
    {
        const int iterations = AuthConstants.PasswordHashIterations;
        byte[] salt = [0x53, 0x68, 0x61, 0x68, 0x43, 0x6F, 0x72, 0x70, 0x53, 0x65, 0x65, 0x64, 0x32, 0x30, 0x32, 0x36];
        var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, 32);

        var output = new byte[13 + salt.Length + subkey.Length];
        output[0] = 0x01;
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(1), 2); // 2 = HMACSHA512
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(5), iterations);
        BinaryPrimitives.WriteUInt32BigEndian(output.AsSpan(9), (uint)salt.Length);
        salt.CopyTo(output, 13);
        subkey.CopyTo(output, 13 + salt.Length);
        return Convert.ToBase64String(output);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateKeyException(ex);
        }
    }
}
