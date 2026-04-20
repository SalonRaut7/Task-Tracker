using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsQuery : IRequest<IReadOnlyList<OrganizationDto>>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.OrganizationsView;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
