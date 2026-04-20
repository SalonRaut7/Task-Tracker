using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Commands.CreateTask;

public class CreateTaskCommand : IRequest<TaskDto>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksCreate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, ProjectId)];

    public Guid ProjectId { get; set; }
    public Guid? EpicId { get; set; }
    public Guid? SprintId { get; set; }
    public string? AssigneeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Status Status { get; set; } = Status.NotStarted;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}