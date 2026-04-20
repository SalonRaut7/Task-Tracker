using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Sprints.Queries.GetSprints;

public sealed class GetSprintsQuery : IRequest<IReadOnlyList<SprintDto>>, IAuthorizedRequest
{
    public Guid? ProjectId { get; set; }

    public string RequiredPermission => AppPermissions.SprintsView;
    public IReadOnlyList<ResourceScope> Scopes =>
        ProjectId.HasValue
            ? [new ResourceScope(ResourceType.Project, ProjectId.Value)]
            : [];
}
