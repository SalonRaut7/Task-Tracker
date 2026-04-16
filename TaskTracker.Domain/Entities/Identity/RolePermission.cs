namespace TaskTracker.Domain.Entities.Identity;

/// Maps a role to a granular permission string (many-to-many).
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

    // Navigation
    public ApplicationRole Role { get; set; } = null!;
}
