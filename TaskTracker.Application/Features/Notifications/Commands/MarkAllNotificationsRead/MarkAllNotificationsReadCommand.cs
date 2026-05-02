using MediatR;
using TaskTracker.Application.Authorization;

namespace TaskTracker.Application.Features.Notifications.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommand : IRequest<Unit>, IAuthenticatedRequest
{
}
