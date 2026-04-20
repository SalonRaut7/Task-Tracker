using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizationById;

public sealed class GetOrganizationByIdQuery : IRequest<OrganizationDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.OrganizationsView;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Organization, Id)];
}
