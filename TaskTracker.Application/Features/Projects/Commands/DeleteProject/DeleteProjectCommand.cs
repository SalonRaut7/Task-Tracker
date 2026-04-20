using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommand : IRequest<bool>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.ProjectsDelete;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, Id)];
}
