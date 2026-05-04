using Microsoft.AspNetCore.SignalR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Infrastructure.Hubs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Services;
// SignalR-backed implementation of INotificationPushService.
// Routes messages to project groups and individual users.
public class NotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationPushService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToProjectAsync(Guid projectId, NotificationDto notification, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"project-{projectId}")
            .SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendToOrganizationAsync(Guid organizationId, NotificationDto notification, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"organization-{organizationId}")
            .SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task SendToUserAsync(string userId, NotificationDto notification, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("ReceiveNotification", notification, ct);
    }

    public async Task BroadcastTaskCreatedAsync(Guid projectId, TaskDto task, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"project-{projectId}")
            .SendAsync("TaskCreated", task, ct);
    }

    public async Task BroadcastTaskCreatedToUserAsync(string userId, TaskDto task, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("TaskCreated", task, ct);
    }

    public async Task BroadcastTaskUpdatedAsync(Guid projectId, TaskDto task, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"project-{projectId}")
            .SendAsync("TaskUpdated", task, ct);
    }

    public async Task BroadcastTaskUpdatedToUserAsync(string userId, TaskDto task, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("TaskUpdated", task, ct);
    }

    public async Task BroadcastTaskDeletedAsync(Guid projectId, int taskId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"project-{projectId}")
            .SendAsync("TaskDeleted", taskId, projectId, ct);
    }

    public async Task BroadcastTaskDeletedToUserAsync(string userId, int taskId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("TaskDeleted", taskId, ct);
    }

    public async Task BroadcastTaskCommentsChangedAsync(Guid projectId, int taskId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"project-{projectId}")
            .SendAsync("TaskCommentsChanged", new { projectId, taskId }, ct);
    }

    public async Task BroadcastScopeMembersChangedAsync(ScopeType scopeType, Guid scopeId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"{scopeType.ToString().ToLowerInvariant()}-{scopeId}")
            .SendAsync("ScopeMembersChanged", new { scopeType = scopeType.ToString(), scopeId }, ct);
    }

    public async Task BroadcastUserWorkspaceChangedAsync(string userId, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("UserWorkspaceChanged", new { userId }, ct);
    }

    public async Task SendUserArchivedAsync(string userId, string? reason, CancellationToken ct = default)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("UserArchived", new { reason }, ct);
    }
}
