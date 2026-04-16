using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Domain.Entities.Identity;

/// Extended Identity role with metadata and permission mapping.
public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
