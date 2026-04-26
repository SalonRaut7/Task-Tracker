using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

public class UserProject
{
    public string UserId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }

    /// The user's scoped role within this project (e.g. ProjectManager, Developer, Viewer).
    public string Role { get; set; } = string.Empty;

    /// Who invited this user (null for auto-assigned creator roles).
    public string? InvitedByUserId { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ApplicationUser? InvitedByUser { get; set; }
}