using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Epics.Commands.UpdateEpic;

public sealed class UpdateEpicCommand : IRequest<EpicDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Status Status { get; set; } = Status.NotStarted;

    public string RequiredPermission => AppPermissions.EpicsUpdate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Epic, Id)];
}
