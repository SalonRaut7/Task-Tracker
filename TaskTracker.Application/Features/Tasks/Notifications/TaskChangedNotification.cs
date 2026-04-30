using MediatR;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Features.Tasks.Notifications;

/// <summary>
/// MediatR notification published after task create/update/delete.
/// Consumed by TaskChangedNotificationHandler to persist and push via SignalR.
/// </summary>
public class TaskChangedNotification : INotification
{
    /// <summary>Created, Updated, Deleted, StatusChanged, Reassigned</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>The task DTO after the change (null on delete).</summary>
    public TaskDto? Task { get; set; }

    /// <summary>Task ID (always available, even on delete).</summary>
    public int TaskId { get; set; }

    /// <summary>Task title for notification message (available even on delete).</summary>
    public string TaskTitle { get; set; } = string.Empty;

    /// <summary>Project the task belongs to.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>User who performed the action.</summary>
    public string ActorUserId { get; set; } = string.Empty;

    /// <summary>Display name of the actor.</summary>
    public string ActorName { get; set; } = string.Empty;

    /// <summary>Previous assignee (for reassignment detection).</summary>
    public string? OldAssigneeId { get; set; }

    /// <summary>New assignee (for reassignment detection).</summary>
    public string? NewAssigneeId { get; set; }

    /// <summary>Previous status value (for status-change detection).</summary>
    public int? OldStatus { get; set; }

    /// <summary>New status value (for status-change detection).</summary>
    public int? NewStatus { get; set; }

    /// <summary>Human-readable field changes included in update notifications.</summary>
    public IReadOnlyList<TaskFieldChange> ChangedFields { get; set; } = [];
}

public sealed class TaskFieldChange
{
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
