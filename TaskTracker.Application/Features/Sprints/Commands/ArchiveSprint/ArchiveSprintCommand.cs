using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Sprints.Commands.ArchiveSprint;

public sealed class ArchiveSprintCommand : IRequest<SprintDto>, IAuthorizedRequest
{
    public Guid Id { get; set; }
    public string ArchiveReason { get; set; } = string.Empty;
    public string RequiredPermission => AppPermissions.SprintsManage;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Sprint, Id)];
}
