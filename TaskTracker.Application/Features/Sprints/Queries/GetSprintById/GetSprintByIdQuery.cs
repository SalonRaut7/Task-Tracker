using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Sprints.Queries.GetSprintById;

public sealed class GetSprintByIdQuery : IRequest<SprintDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.SprintsView;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Sprint, Id)];
}
