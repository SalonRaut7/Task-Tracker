using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

public static class TaskAttachmentDtoMapper
{
    public static TaskAttachmentDto ToDto(TaskAttachment attachment)
    {
        return new TaskAttachmentDto
        {
            Id = attachment.Id,
            TaskId = attachment.TaskId,
            UploaderId = attachment.UploaderId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            Url = attachment.CloudinaryUrl,
            CreatedAt = attachment.CreatedAt,
        };
    }

    public static Expression<Func<TaskAttachment, TaskAttachmentDto>> Projection()
    {
        return attachment => new TaskAttachmentDto
        {
            Id = attachment.Id,
            TaskId = attachment.TaskId,
            UploaderId = attachment.UploaderId,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            Url = attachment.CloudinaryUrl,
            CreatedAt = attachment.CreatedAt,
        };
    }
}