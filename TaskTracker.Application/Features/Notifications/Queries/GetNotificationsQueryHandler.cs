using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Notifications.Queries;

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
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var notifications = await _notificationRepository.GetByUserIdAsync(
            _currentUser.UserId, request.Take, cancellationToken);

        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            RecipientUserId = n.RecipientUserId,
            ActorUserId = n.ActorUserId,
            ActorName = n.ActorName,
            Type = n.Type,
            Message = n.Message,
            TaskId = n.TaskId,
            ProjectId = n.ProjectId,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
        }).ToList();
    }
}
