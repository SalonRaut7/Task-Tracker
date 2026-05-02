using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

// Server-persisted notification pushed via SignalR.
// Tracks task lifecycle events (created, updated, deleted, reassigned, due-soon, overdue).

public class Notification
{
    public Guid Id { get; private set; }

    //FK → ApplicationUser.Id — who receives this notification.
    public string RecipientUserId { get; private set; } = string.Empty;

    // FK → ApplicationUser.Id — who triggered the event (null for system events).
    public string? ActorUserId { get; private set; }

    // Denormalized actor display name for fast rendering.
    public string ActorName { get; private set; } = string.Empty;
    // Event type discriminator:
    // TaskCreated, TaskUpdated, TaskDeleted, TaskReassigned, TaskDueSoon, TaskOverdue
    public string Type { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    // Nullable FK — links to the task for click-through navigation.
    public int? TaskId { get; private set; }
    // Project context for group routing.
    public Guid? ProjectId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }
    // Navigation properties
    public ApplicationUser Recipient { get; set; } = null!;
    public ApplicationUser? Actor { get; set; }

    // Required by EF Core
    private Notification() { }

    public static Notification Create(
        string recipientUserId,
        string? actorUserId,
        string actorName,
        string type,
        string message,
        int? taskId,
        Guid? projectId,
        bool isRead,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(recipientUserId))
        {
            throw new InvalidOperationException("Recipient user is required.");
        }

        if (string.IsNullOrWhiteSpace(actorName))
        {
            throw new InvalidOperationException("Actor name is required.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new InvalidOperationException("Notification type is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException("Notification message is required.");
        }

        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId.Trim(),
            ActorUserId = string.IsNullOrWhiteSpace(actorUserId) ? null : actorUserId.Trim(),
            ActorName = actorName.Trim(),
            Type = type.Trim(),
            Message = message.Trim(),
            TaskId = taskId,
            ProjectId = projectId,
            IsRead = isRead,
            CreatedAt = NormalizeUtc(createdAtUtc)
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
