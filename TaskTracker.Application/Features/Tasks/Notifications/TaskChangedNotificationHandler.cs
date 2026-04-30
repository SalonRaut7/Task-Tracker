using MediatR;
using Microsoft.Extensions.Logging;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Tasks.Notifications;

/// Handles TaskChangedNotification by:
/// 1. Building human-readable notification messages
/// 2. Persisting Notification records for each project member (excluding actor)
/// 3. Pushing via INotificationPushService (SignalR) to project groups
/// 4. Broadcasting task data changes for live dashboard/task-list sync
public class TaskChangedNotificationHandler : INotificationHandler<TaskChangedNotification>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationPushService _pushService;
    private readonly ILogger<TaskChangedNotificationHandler> _logger;

    public TaskChangedNotificationHandler(
        INotificationRepository notificationRepository,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        INotificationPushService pushService,
        ILogger<TaskChangedNotificationHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Handle(TaskChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var message = BuildMessage(notification);
            var notificationType = ResolveNotificationType(notification);

            // Get all project members to determine recipients
            var projectMembers = await _membershipRepository.GetProjectMembershipsAsync(
                notification.ProjectId, cancellationToken);

            var projectMemberUserIds = projectMembers
                .Select(m => m.UserId)
                .Distinct()
                .ToList();
            var superAdminUserIds = await _userRepository.GetSuperAdminUserIdsAsync(cancellationToken);
            var recipientUserIds = projectMemberUserIds
                .Concat(superAdminUserIds)
                .Distinct()
                .ToList();
            var superAdminsOutsideProject = superAdminUserIds
                .Except(projectMemberUserIds, StringComparer.Ordinal)
                .ToList();

            // Persist notifications for each recipient
            var notificationEntities = recipientUserIds.Select(recipientId => new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientId,
                ActorUserId = notification.ActorUserId,
                ActorName = notification.ActorName,
                Type = notificationType,
                Message = message,
                TaskId = notification.TaskId,
                ProjectId = notification.ProjectId,
                IsRead = string.Equals(recipientId, notification.ActorUserId, StringComparison.Ordinal),
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            if (notificationEntities.Count > 0)
            {
                await _notificationRepository.AddRangeAsync(notificationEntities, cancellationToken);
            }

            // Build DTO for the SignalR push (use first entity as template)
            var pushDto = new NotificationDto
            {
                Id = Guid.NewGuid(), // Each client gets a unique push ID
                ActorUserId = notification.ActorUserId,
                ActorName = notification.ActorName,
                Type = notificationType,
                Message = message,
                TaskId = notification.TaskId,
                ProjectId = notification.ProjectId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            };

            // Push notification to project group (all members see it)
            await _pushService.SendToProjectAsync(notification.ProjectId, pushDto, cancellationToken);

            // SuperAdmins not in the project group still need a real-time push.
            foreach (var userId in superAdminsOutsideProject)
            {
                await _pushService.SendToUserAsync(
                    userId,
                    new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = notification.ActorUserId,
                        ActorName = notification.ActorName,
                        Type = notificationType,
                        Message = message,
                        TaskId = notification.TaskId,
                        ProjectId = notification.ProjectId,
                        IsRead = string.Equals(userId, notification.ActorUserId, StringComparison.Ordinal),
                        CreatedAt = DateTime.UtcNow,
                    },
                    cancellationToken);
            }

            // If task was reassigned, send a direct notification to the new assignee
            if (notification.EventType == "Reassigned" && !string.IsNullOrWhiteSpace(notification.NewAssigneeId))
            {
                var reassignDto = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = notification.ActorUserId,
                    ActorName = notification.ActorName,
                    Type = "TaskReassigned",
                    Message = $"TASK-{notification.TaskId} was assigned to you by {notification.ActorName}",
                    TaskId = notification.TaskId,
                    ProjectId = notification.ProjectId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                };

                await _pushService.SendToUserAsync(notification.NewAssigneeId, reassignDto, cancellationToken);
            }

            // Broadcast task data changes for live dashboard/task-list sync
            switch (notification.EventType)
            {
                case "Created" when notification.Task is not null:
                    await _pushService.BroadcastTaskCreatedAsync(
                        notification.ProjectId, notification.Task, cancellationToken);
                    foreach (var userId in superAdminsOutsideProject)
                    {
                        await _pushService.BroadcastTaskCreatedToUserAsync(
                            userId, notification.Task, cancellationToken);
                    }
                    break;

                case "Updated" when notification.Task is not null:
                case "StatusChanged" when notification.Task is not null:
                case "Reassigned" when notification.Task is not null:
                    await _pushService.BroadcastTaskUpdatedAsync(
                        notification.ProjectId, notification.Task, cancellationToken);
                    foreach (var userId in superAdminsOutsideProject)
                    {
                        await _pushService.BroadcastTaskUpdatedToUserAsync(
                            userId, notification.Task, cancellationToken);
                    }
                    break;

                case "Deleted":
                    await _pushService.BroadcastTaskDeletedAsync(
                        notification.ProjectId, notification.TaskId, cancellationToken);
                    foreach (var userId in superAdminsOutsideProject)
                    {
                        await _pushService.BroadcastTaskDeletedToUserAsync(
                            userId, notification.TaskId, cancellationToken);
                    }
                    break;
            }

            // Prune old notifications for each recipient
            foreach (var recipientId in recipientUserIds)
            {
                await _notificationRepository.PruneAsync(recipientId, cancellationToken);
            }

            _logger.LogInformation(
                "Published {EventType} notification for TASK-{TaskId} in project {ProjectId} to {RecipientCount} recipients",
                notification.EventType, notification.TaskId, notification.ProjectId, recipientUserIds.Count);
        }
        catch (Exception ex)
        {
            // Never let notification failures break the main request pipeline
            _logger.LogError(ex,
                "Failed to publish {EventType} notification for TASK-{TaskId}",
                notification.EventType, notification.TaskId);
        }
    }

    private static string BuildMessage(TaskChangedNotification notification)
    {
        var taskRef = $"TASK-{notification.TaskId}";
        var taskLabel = string.IsNullOrWhiteSpace(notification.TaskTitle)
            ? taskRef
            : $"{notification.TaskTitle} ({taskRef})";

        return notification.EventType switch
        {
            "Created" => $"{notification.ActorName} created {taskLabel}",
            "Updated" => $"{notification.ActorName} updated {taskLabel}",
            "Deleted" => $"{notification.ActorName} deleted {taskLabel}",
            "StatusChanged" => BuildUpdateMessage(notification, taskLabel, "changed status of"),
            "Reassigned" => BuildUpdateMessage(notification, taskLabel, "reassigned"),
            _ => $"{notification.ActorName} modified {taskLabel}",
        };
    }

    private static string BuildUpdateMessage(
        TaskChangedNotification notification,
        string taskLabel,
        string actionVerb)
    {
        var changeDetails = BuildChangeDetails(notification.ChangedFields);

        if (changeDetails.Count == 0)
        {
            return $"{notification.ActorName} {actionVerb} {taskLabel}";
        }

        if (string.Equals(actionVerb, "reassigned", StringComparison.OrdinalIgnoreCase))
        {
            return $"{notification.ActorName} reassigned {taskLabel}: {string.Join("; ", changeDetails.Select(FormatChange))}";
        }

        if (changeDetails.Count == 1 &&
            string.Equals(changeDetails[0].FieldName, "Description", StringComparison.OrdinalIgnoreCase))
        {
            return $"{notification.ActorName} updated {taskLabel}: Description updated to {changeDetails[0].NewValue}";
        }

        if (changeDetails.Count == 1 &&
            string.Equals(changeDetails[0].FieldName, "Status", StringComparison.OrdinalIgnoreCase))
        {
            return $"{notification.ActorName} changed status of {taskLabel} from {changeDetails[0].OldValue} to {changeDetails[0].NewValue}";
        }

        return $"{notification.ActorName} {actionVerb} {taskLabel}: {string.Join("; ", changeDetails.Select(FormatChange))}";
    }

    private static List<TaskFieldChange> BuildChangeDetails(IReadOnlyList<TaskFieldChange> changes)
    {
        return changes
            .Where(change => !string.Equals(change.FieldName, "Epic", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(change.FieldName, "Sprint", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string FormatChange(TaskFieldChange change)
    {
        return change.FieldName switch
        {
            "Title" when !string.IsNullOrWhiteSpace(change.NewValue) => $"Title updated to {change.NewValue}",
            "Description" when string.IsNullOrWhiteSpace(change.NewValue) => "Description cleared",
            "Description" => $"Description updated to {change.NewValue}",
            "Status" => $"Status changed from {change.OldValue} to {change.NewValue}",
            "Priority" => $"Priority changed from {change.OldValue} to {change.NewValue}",
            "StartDate" => $"Start date changed from {change.OldValue} to {change.NewValue}",
            "EndDate" => $"End date changed from {change.OldValue} to {change.NewValue}",
            _ when !string.IsNullOrWhiteSpace(change.NewValue) => $"{change.FieldName} updated to {change.NewValue}",
            _ => $"{change.FieldName} changed",
        };
    }

    private static string ResolveNotificationType(TaskChangedNotification notification)
    {
        return notification.EventType switch
        {
            "Created" => "TaskCreated",
            "Updated" => "TaskUpdated",
            "Deleted" => "TaskDeleted",
            "StatusChanged" => "TaskUpdated",
            "Reassigned" => "TaskReassigned",
            _ => "TaskUpdated",
        };
    }
}
