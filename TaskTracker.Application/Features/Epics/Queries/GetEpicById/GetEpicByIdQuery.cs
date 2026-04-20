using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Epics.Queries.GetEpicById;

public sealed class GetEpicByIdQuery : IRequest<EpicDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.EpicsView;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Epic, Id)];
}
