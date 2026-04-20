using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjectById;

public sealed class GetProjectByIdQuery : IRequest<ProjectDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.ProjectsView;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, Id)];
}
