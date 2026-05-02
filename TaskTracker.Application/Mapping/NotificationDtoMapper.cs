using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

internal static class NotificationDtoMapper
{
    public static NotificationDto ToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            RecipientUserId = notification.RecipientUserId,
            ActorUserId = notification.ActorUserId,
            ActorName = notification.ActorName,
            Type = notification.Type,
            Message = notification.Message,
            TaskId = notification.TaskId,
            ProjectId = notification.ProjectId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
        };
    }
}
