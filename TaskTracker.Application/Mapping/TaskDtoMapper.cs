using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
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
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
        };
    }

    public static TaskDto ToDto(TaskSnapshot snapshot)
    {
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
            CreatedAt = snapshot.CreatedAt,
            UpdatedAt = snapshot.UpdatedAt,
        };
    }

    public static Expression<Func<TaskItem, TaskDto>> Projection()
    {
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
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
        };
    }
}
