using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Interfaces;
// Abstraction for pushing real-time notifications to connected clients.
// Infrastructure implements this via SignalR IHubContext.
// Keeps Application layer free of direct Hub dependencies.

public interface INotificationPushService
{
    // Push a notification to a project group.
    Task SendToProjectAsync(Guid projectId, NotificationDto notification, CancellationToken ct = default);
    // Push a notification to an organization group.
    Task SendToOrganizationAsync(Guid organizationId, NotificationDto notification, CancellationToken ct = default);
    // Push a notification to a specific user.
    Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default);

    // Push a TaskCreated event to a project group for live data sync.
    Task BroadcastTaskCreatedAsync(Guid projectId, TaskDto task, CancellationToken ct = default);
    // Push a TaskCreated event to a specific user group for live data sync.
    Task BroadcastTaskCreatedToUserAsync(string userId, TaskDto task, CancellationToken ct = default);

    // Push a TaskUpdated event to a project group for live data sync.
    Task BroadcastTaskUpdatedAsync(Guid projectId, TaskDto task, CancellationToken ct = default);
    // Push a TaskUpdated event to a specific user group for live data sync.
    Task BroadcastTaskUpdatedToUserAsync(string userId, TaskDto task, CancellationToken ct = default);

    // Push a TaskDeleted event to a project group for live data sync.
    Task BroadcastTaskDeletedAsync(Guid projectId, int taskId, CancellationToken ct = default);
    // Push a TaskDeleted event to a specific user group for live data sync.
    Task BroadcastTaskDeletedToUserAsync(string userId, int taskId, CancellationToken ct = default);

    // Push a comment-thread refresh event to a project group.
    Task BroadcastTaskCommentsChangedAsync(Guid projectId, int taskId, CancellationToken ct = default);

    // Push a member/invitation refresh event to a scope group.
    Task BroadcastScopeMembersChangedAsync(ScopeType scopeType, Guid scopeId, CancellationToken ct = default);

    // Push a direct workspace refresh event to a specific user group.
    Task BroadcastUserWorkspaceChangedAsync(string userId, CancellationToken ct = default);
}
