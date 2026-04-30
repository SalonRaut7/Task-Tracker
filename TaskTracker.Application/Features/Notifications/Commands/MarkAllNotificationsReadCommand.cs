using MediatR;

namespace TaskTracker.Application.Features.Notifications.Commands;

public class MarkAllNotificationsReadCommand : IRequest<Unit>
{
}
