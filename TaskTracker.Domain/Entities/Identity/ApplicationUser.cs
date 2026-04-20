using Microsoft.AspNetCore.Identity;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Entities.Identity;
/// Extended Identity user with application-specific properties.
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<OtpEntry> OtpEntries { get; set; } = new List<OtpEntry>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> ReportedTasks { get; set; } = new List<TaskItem>();
    public ICollection<UserOrganization> OrganizationMemberships { get; set; } = new List<UserOrganization>();
    public ICollection<UserProject> ProjectMemberships { get; set; } = new List<UserProject>();
}
