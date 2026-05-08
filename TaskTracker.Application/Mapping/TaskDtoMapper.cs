using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Events;

namespace TaskTracker.Application.Mapping;

public static class TaskDtoMapper
{
    public static TaskDto ToDto(TaskItem task)
    {
        return new TaskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            EpicId = task.EpicId,
            SprintId = task.SprintId,
            AssigneeId = task.AssigneeId,
            ReporterId = task.ReporterId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            TaskCode = task.TaskCode,
            IsExpired = task.IsExpired,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
        };
    }

    public static TaskDto ToDto(TaskSnapshot snapshot)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return new TaskDto
        {
            Id = snapshot.Id,
            ProjectId = snapshot.ProjectId,
            EpicId = snapshot.EpicId,
            SprintId = snapshot.SprintId,
            AssigneeId = snapshot.AssigneeId,
            ReporterId = snapshot.ReporterId,
            Title = snapshot.Title,
            Description = snapshot.Description,
            Status = snapshot.Status,
            Priority = snapshot.Priority,
            StartDate = snapshot.StartDate,
            EndDate = snapshot.EndDate,
            TaskCode = snapshot.TaskCode,
            IsExpired = snapshot.EndDate.HasValue
                && snapshot.EndDate.Value < today
                && snapshot.Status != Status.Completed
                && snapshot.Status != Status.Cancelled,
            CreatedAt = snapshot.CreatedAt,
            UpdatedAt = snapshot.UpdatedAt,
        };
    }

    public static Expression<Func<TaskItem, TaskDto>> Projection()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return task => new TaskDto
        {
            Id = task.Id,
            ProjectId = task.ProjectId,
            EpicId = task.EpicId,
            SprintId = task.SprintId,
            AssigneeId = task.AssigneeId,
            ReporterId = task.ReporterId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            StartDate = task.StartDate,
            EndDate = task.EndDate,
            TaskCode = task.TaskCode,
            IsExpired = task.EndDate.HasValue
                && task.EndDate.Value < today
                && task.Status != Status.Completed
                && task.Status != Status.Cancelled,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
        };
    }
}
