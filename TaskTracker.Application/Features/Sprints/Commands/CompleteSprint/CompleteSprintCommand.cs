using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommand : IRequest<CompleteSprintResult>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.SprintsManage;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Sprint, Id)];
}
