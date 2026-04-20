using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommand : IRequest<ProjectDto>, IAuthorizedRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string RequiredPermission => AppPermissions.ProjectsCreate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Organization, OrganizationId)];
}
