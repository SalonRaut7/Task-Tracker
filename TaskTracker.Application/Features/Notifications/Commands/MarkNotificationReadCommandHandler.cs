using MediatR;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Notifications.Commands;

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
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        return await _notificationRepository.MarkAsReadAsync(
            request.NotificationId, _currentUser.UserId, cancellationToken);
    }
}
