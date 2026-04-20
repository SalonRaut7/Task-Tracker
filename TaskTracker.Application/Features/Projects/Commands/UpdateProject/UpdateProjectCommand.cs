using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommand : IRequest<ProjectDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string RequiredPermission => AppPermissions.ProjectsUpdate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, Id)];
}
