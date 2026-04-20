using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

public class UserProject
{
    public string UserId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}