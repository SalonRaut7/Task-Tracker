using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

public class UserOrganization
{
    public string UserId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}