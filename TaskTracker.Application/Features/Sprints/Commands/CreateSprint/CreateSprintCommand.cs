using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Sprints.Commands.CreateSprint;

public sealed class CreateSprintCommand : IRequest<SprintDto>, IAuthorizedRequest
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    // Status is intentionally omitted — new sprints always start as Planning.
    public string RequiredPermission => AppPermissions.SprintsCreate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, ProjectId)];
}
