using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQuery : IRequest<List<TaskDto>>
{
    public string? TitleContains { get; set; }
    public Status? Status { get; set; }
}