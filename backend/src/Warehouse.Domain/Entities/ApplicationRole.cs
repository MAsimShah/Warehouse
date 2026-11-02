using Microsoft.AspNetCore.Identity;

namespace Warehouse.Domain.Entities;

// Table only for now. No flow uses roles yet. Roles are system-wide.
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
