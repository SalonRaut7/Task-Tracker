using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly INotificationDispatchService _notificationDispatchService;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationPushService _pushService;
    private readonly ILogger<TaskChangedNotificationHandler> _logger;

    public TaskChangedNotificationHandler(
        INotificationDispatchService notificationDispatchService,
        IMembershipRepository membershipRepository,
        IUserRepository userRepository,
        INotificationPushService pushService,
        ILogger<TaskChangedNotificationHandler> logger)
    {
        _notificationDispatchService = notificationDispatchService;
        _membershipRepository = membershipRepository;
        _userRepository = userRepository;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Handle(TaskChangedNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var actorName = notification.ActorName;
            if (string.IsNullOrWhiteSpace(actorName) && !string.IsNullOrWhiteSpace(notification.ActorUserId))
            {
                actorName = await _userRepository.GetFullNameAsync(notification.ActorUserId, cancellationToken) ?? "Unknown";
            }

            var message = BuildMessage(notification, actorName);
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
            var nowUtc = DateTime.UtcNow;
            var notificationEntities = recipientUserIds.Select(recipientId => Notification.Create(
                recipientId,
                notification.ActorUserId,
                actorName,
                notificationType,
                message,
                notification.TaskId,
                notification.ProjectId,
                string.Equals(recipientId, notification.ActorUserId, StringComparison.Ordinal),
                nowUtc)).ToList();

            await _notificationDispatchService.DispatchAsync(notificationEntities, cancellationToken);

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

    private static string BuildMessage(TaskChangedNotification notification, string actorName)
    {
        var taskRef = $"TASK-{notification.TaskId}";
        var taskLabel = string.IsNullOrWhiteSpace(notification.TaskTitle)
            ? taskRef
            : $"{notification.TaskTitle} ({taskRef})";

        return notification.EventType switch
        {
            "Created" => $"{actorName} created {taskLabel}",
            "Updated" => $"{actorName} updated {taskLabel}",
            "Deleted" => $"{actorName} deleted {taskLabel}",
            "StatusChanged" => BuildUpdateMessage(notification, taskLabel, "changed status of", actorName),
            "Reassigned" => BuildUpdateMessage(notification, taskLabel, "reassigned", actorName),
            _ => $"{actorName} modified {taskLabel}",
        };
    }

    private static string BuildUpdateMessage(
        TaskChangedNotification notification,
        string taskLabel,
        string actionVerb,
        string actorName)
    {
        var changeDetails = BuildChangeDetails(notification.ChangedFields);

        if (changeDetails.Count == 0)
        {
            return $"{actorName} {actionVerb} {taskLabel}";
        }

        if (string.Equals(actionVerb, "reassigned", StringComparison.OrdinalIgnoreCase))
        {
            return $"{actorName} reassigned {taskLabel}: {string.Join("; ", changeDetails.Select(FormatChange))}";
        }

        if (changeDetails.Count == 1 &&
            string.Equals(changeDetails[0].FieldName, "Description", StringComparison.OrdinalIgnoreCase))
        {
            return $"{actorName} updated {taskLabel}: Description updated to {changeDetails[0].NewValue}";
        }

        if (changeDetails.Count == 1 &&
            string.Equals(changeDetails[0].FieldName, "Status", StringComparison.OrdinalIgnoreCase))
        {
            return $"{actorName} changed status of {taskLabel} from {changeDetails[0].OldValue} to {changeDetails[0].NewValue}";
        }

        return $"{actorName} {actionVerb} {taskLabel}: {string.Join("; ", changeDetails.Select(FormatChange))}";
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
