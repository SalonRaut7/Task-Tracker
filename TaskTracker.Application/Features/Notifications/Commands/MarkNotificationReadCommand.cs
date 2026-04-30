using MediatR;

namespace TaskTracker.Application.Features.Notifications.Commands;

public class MarkNotificationReadCommand : IRequest<bool>
{
    public Guid NotificationId { get; set; }
}
