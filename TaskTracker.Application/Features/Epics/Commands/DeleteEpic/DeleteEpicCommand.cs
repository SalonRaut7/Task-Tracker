using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Epics.Commands.DeleteEpic;

public sealed class DeleteEpicCommand : IRequest<bool>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.EpicsDelete;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Epic, Id)];
}
