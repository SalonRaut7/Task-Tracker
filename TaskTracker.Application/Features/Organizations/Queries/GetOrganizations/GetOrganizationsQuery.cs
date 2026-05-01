using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsQuery : IRequest<PagedResultDto<OrganizationDto>>, IAuthorizedRequest
{
    public string? Search { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }

    public string RequiredPermission => AppPermissions.OrganizationsView;
    public IReadOnlyList<ResourceScope> Scopes => [];
}
