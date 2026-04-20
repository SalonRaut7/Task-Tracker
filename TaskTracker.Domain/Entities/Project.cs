namespace TaskTracker.Domain.Entities;

/// Represents a project within an organization (like a Jira project).
/// Contains sprints and epics.

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;            // e.g. "PROJ", "TRK"
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Organization Organization { get; set; } = null!;
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public ICollection<Epic> Epics { get; set; } = new List<Epic>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<UserProject> UserMemberships { get; set; } = new List<UserProject>();
}
