using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Infrastructure.Hubs;

/// SignalR hub for real-time task notifications.
/// Server-to-client push only — no client-callable methods.
[Authorize]
public class NotificationHub : Hub
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        IMembershipRepository membershipRepository,
        ILogger<NotificationHub> logger)
    {
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("SignalR connection attempted without user identity");
            Context.Abort();
            return;
        }

        // Add user to project-based groups for scoped notifications
        var organizationIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId);
        foreach (var organizationId in organizationIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"organization-{organizationId}");
        }

        // Add user to project-based groups for scoped notifications
        var projectIds = await _membershipRepository.GetUserProjectIdsAsync(userId);
        foreach (var projectId in projectIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
        }

        // Also add to a personal group for direct notifications (e.g., reassignment)
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        _logger.LogInformation(
            "User {UserId} connected to NotificationHub. Joined {OrgGroupCount} organization groups and {ProjectGroupCount} project groups",
            userId, organizationIds.Count, projectIds.Count);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
        await base.OnDisconnectedAsync(exception);
    }
}
