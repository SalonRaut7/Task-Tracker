using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

// Server-persisted notification pushed via SignalR.
// Tracks task lifecycle events (created, updated, deleted, reassigned, due-soon, overdue).

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    //FK → ApplicationUser.Id — who receives this notification.
    public string RecipientUserId { get; set; } = string.Empty;

    // FK → ApplicationUser.Id — who triggered the event (null for system events).
    public string? ActorUserId { get; set; }

    // Denormalized actor display name for fast rendering.
    public string ActorName { get; set; } = string.Empty;
    // Event type discriminator:
    // TaskCreated, TaskUpdated, TaskDeleted, TaskReassigned, TaskDueSoon, TaskOverdue
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    // Nullable FK — links to the task for click-through navigation.
    public int? TaskId { get; set; }
    // Project context for group routing.
    public Guid? ProjectId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // Navigation properties
    public ApplicationUser Recipient { get; set; } = null!;
    public ApplicationUser? Actor { get; set; }
}
