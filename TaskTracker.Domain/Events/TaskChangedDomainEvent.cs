using MediatR;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Events;

/// Domain event raised when a task is created, updated, reassigned, status-changed, or deleted.
public sealed class TaskChangedDomainEvent : INotification
{
    public string EventType { get; init; } = string.Empty;
    public int TaskId { get; init; }
    public string TaskTitle { get; init; } = string.Empty;
    public Guid ProjectId { get; init; }
    public string ActorUserId { get; init; } = string.Empty;
    public string? OldAssigneeId { get; init; }
    public string? NewAssigneeId { get; init; }
    public int? OldStatus { get; init; }
    public int? NewStatus { get; init; }
    public IReadOnlyList<TaskChangedField> ChangedFields { get; init; } = [];
    public TaskSnapshot? Task { get; init; }
    public TaskItem? TaskEntity { get; init; }
}

public sealed class TaskChangedField
{
    public string FieldName { get; init; } = string.Empty;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

public sealed class TaskSnapshot
{
    public int Id { get; init; }
    public Guid ProjectId { get; init; }
    public Guid? EpicId { get; init; }
    public Guid? SprintId { get; init; }
    public string? AssigneeId { get; init; }
    public string ReporterId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Status Status { get; init; }
    public TaskPriority Priority { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
