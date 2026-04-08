using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommand : IRequest<TaskDto>
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Status Status { get; set; } = Status.NotStarted;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}