using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Organizations.Commands.DeleteOrganization;

public sealed class DeleteOrganizationCommand : IRequest<bool>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.OrganizationsDelete;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Organization, Id)];
}
