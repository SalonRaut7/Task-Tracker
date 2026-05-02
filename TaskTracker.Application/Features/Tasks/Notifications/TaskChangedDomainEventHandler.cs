using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Events;

namespace TaskTracker.Application.Features.Tasks.Notifications;

/// Transitional bridge: converts domain events into existing application notifications
/// so downstream real-time/persistence behavior stays contract-compatible.
public sealed class TaskChangedDomainEventHandler : INotificationHandler<TaskChangedDomainEvent>
{
    private readonly IPublisher _publisher;

    public TaskChangedDomainEventHandler(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task Handle(TaskChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var taskSnapshot = notification.TaskEntity?.ToSnapshot() ?? notification.Task;
        var taskId = taskSnapshot?.Id ?? notification.TaskEntity?.Id ?? notification.TaskId;
        var taskTitle = string.IsNullOrWhiteSpace(notification.TaskTitle)
            ? taskSnapshot?.Title ?? string.Empty
            : notification.TaskTitle;
        var projectId = taskSnapshot?.ProjectId ?? notification.TaskEntity?.ProjectId ?? notification.ProjectId;

        await _publisher.Publish(new TaskChangedNotification
        {
            EventType = notification.EventType,
            Task = taskSnapshot is null ? null : TaskDtoMapper.ToDto(taskSnapshot),
            TaskId = taskId,
            TaskTitle = taskTitle,
            ProjectId = projectId,
            ActorUserId = notification.ActorUserId,
            OldAssigneeId = notification.OldAssigneeId,
            NewAssigneeId = notification.NewAssigneeId,
            OldStatus = notification.OldStatus,
            NewStatus = notification.NewStatus,
            ChangedFields = notification.ChangedFields.Select(change => new TaskFieldChange
            {
                FieldName = change.FieldName,
                OldValue = change.OldValue,
                NewValue = change.NewValue,
            }).ToList(),
        }, cancellationToken);
    }
}
