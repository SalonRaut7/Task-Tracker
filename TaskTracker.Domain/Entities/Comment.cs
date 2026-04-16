using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

/// A comment on a task, authored by a user.
public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TaskId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TaskItem Task { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
