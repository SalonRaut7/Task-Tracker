using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, List<NotificationDto>>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<List<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        var notifications = await _notificationRepository.GetByUserIdAsync(
            userId, request.Take, cancellationToken);

        return notifications.Select(NotificationDtoMapper.ToDto).ToList();
    }
}
