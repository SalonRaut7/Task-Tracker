using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommand : IRequest<bool>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksDelete;
    public IReadOnlyList<ResourceScope> Scopes => [];

    public int Id { get; set; }
}