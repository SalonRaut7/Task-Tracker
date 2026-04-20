using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQuery : IRequest<IReadOnlyList<ProjectDto>>, IAuthorizedRequest
{
    public Guid? OrganizationId { get; set; }

    public string RequiredPermission => AppPermissions.ProjectsView;
    public IReadOnlyList<ResourceScope> Scopes =>
        OrganizationId.HasValue
            ? [new ResourceScope(ResourceType.Organization, OrganizationId.Value)]
            : [];
}
