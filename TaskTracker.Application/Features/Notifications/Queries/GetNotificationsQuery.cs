using MediatR;
using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Features.Notifications.Queries;

public class GetNotificationsQuery : IRequest<List<NotificationDto>>
{
    public int Take { get; set; } = 50; // Default to latest 50 notifications, can be overridden by caller.
}
