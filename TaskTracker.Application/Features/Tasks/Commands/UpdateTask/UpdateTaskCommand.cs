using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommand : IRequest<TaskDto?>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksUpdate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, ProjectId)];

    public int Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EpicId { get; set; }
    public Guid? SprintId { get; set; }
    public string? AssigneeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Status Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? EndDateExtensionDays { get; set; }
}