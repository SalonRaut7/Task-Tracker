using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Tasks.Queries.GetTaskById;

public class GetTaskByIdQuery : IRequest<TaskDto?>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksView;
    public IReadOnlyList<ResourceScope> Scopes => [];

    public int Id { get; set; }
}