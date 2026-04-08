using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommand : IRequest<TaskDto?>
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Status Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}