using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Queries.GetAllTasks;

public class GetAllTasksQuery : IRequest<PagedResultDto<TaskDto>>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksView;
    public IReadOnlyList<ResourceScope> Scopes => [];

    public string? TitleContains { get; set; }
    public Status? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}