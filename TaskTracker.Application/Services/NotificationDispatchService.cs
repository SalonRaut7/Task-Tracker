using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Services;

public sealed class NotificationDispatchService : INotificationDispatchService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationPushService _notificationPushService;

    public NotificationDispatchService(
        INotificationRepository notificationRepository,
        INotificationPushService notificationPushService)
    {
        _notificationRepository = notificationRepository;
        _notificationPushService = notificationPushService;
    }

    public async Task DispatchAsync(
        IReadOnlyCollection<Notification> notifications,
        CancellationToken cancellationToken = default)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        await _notificationRepository.AddRangeAsync(notifications, cancellationToken);

        foreach (var notification in notifications)
        {
            await _notificationPushService.SendToUserAsync(
                notification.RecipientUserId,
                NotificationDtoMapper.ToDto(notification),
                cancellationToken);
        }

        foreach (var recipientId in notifications.Select(item => item.RecipientUserId).Distinct(StringComparer.Ordinal))
        {
            await _notificationRepository.PruneAsync(recipientId, cancellationToken);
        }
    }
}
