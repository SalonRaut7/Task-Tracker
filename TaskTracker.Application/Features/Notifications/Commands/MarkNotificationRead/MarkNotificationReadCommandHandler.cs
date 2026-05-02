using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, bool>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICurrentUserService _currentUser;

    public MarkNotificationReadCommandHandler(
        INotificationRepository notificationRepository,
        ICurrentUserService currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();
        return await _notificationRepository.MarkAsReadAsync(
            request.NotificationId, userId, cancellationToken);
    }
}
