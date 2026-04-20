using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQuery : IRequest<PagedResultDto<TaskDto>>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksView;
    public IReadOnlyList<ResourceScope> Scopes =>
        ProjectId.HasValue
            ? [new ResourceScope(ResourceType.Project, ProjectId.Value)]
            : [];

    public Guid? ProjectId { get; set; }
    public Guid? EpicId { get; set; }
    public Guid? SprintId { get; set; }
    public string? AssigneeId { get; set; }
    public string? ReporterId { get; set; }
    public string? TitleContains { get; set; }
    public Status? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}