using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Sprints.Commands.StartSprint;

public sealed class StartSprintCommand : IRequest<SprintDto>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.SprintsManage;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Sprint, Id)];
}
