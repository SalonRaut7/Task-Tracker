using MediatR;
using TaskTracker.Application.Authorization;

namespace TaskTracker.Application.Features.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommand : IRequest<bool>, IAuthenticatedRequest
{
    public Guid NotificationId { get; set; }
}
