using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

/// Top-level tenant entity. Projects and memberships belong to an organization.
public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;           // URL-safe unique identifier
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<UserOrganization> UserMemberships { get; set; } = new List<UserOrganization>();
}
