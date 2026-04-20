using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Epics.Queries.GetEpics;

public sealed class GetEpicsQuery : IRequest<IReadOnlyList<EpicDto>>, IAuthorizedRequest
{
    public Guid? ProjectId { get; set; }

    public string RequiredPermission => AppPermissions.EpicsView;
    public IReadOnlyList<ResourceScope> Scopes =>
        ProjectId.HasValue
            ? [new ResourceScope(ResourceType.Project, ProjectId.Value)]
            : [];
}
