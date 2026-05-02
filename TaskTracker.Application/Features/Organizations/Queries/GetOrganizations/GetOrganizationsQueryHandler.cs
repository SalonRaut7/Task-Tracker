using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, PagedResultDto<OrganizationDto>>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public GetOrganizationsQueryHandler(
        IOrganizationRepository organizationRepository,
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _organizationRepository = organizationRepository;
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<PagedResultDto<OrganizationDto>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var query = _organizationRepository.Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            var likePattern = $"%{searchTerm.Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(organization =>
                EF.Functions.ILike(organization.Name, likePattern) ||
                EF.Functions.ILike(organization.Slug, likePattern));
        }

        if (!_currentUser.IsSuperAdmin)
        {
            var userId = _currentUser.UserId!;
            var organizationIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId, cancellationToken);
            if (organizationIds.Count == 0)
            {
                return new PagedResultDto<OrganizationDto>
                {
                    Data = [],
                    TotalCount = 0
                };
            }

            query = query.Where(org => organizationIds.Contains(org.Id));
        }

        query = query.OrderBy(org => org.Name);
        var totalCount = await query.CountAsync(cancellationToken);

        if (request.Skip.HasValue)
        {
            query = query.Skip(Math.Max(0, request.Skip.Value));
        }

        if (request.Take.HasValue && request.Take.Value > 0)
        {
            query = query.Take(request.Take.Value);
        }

        var data = await query
            .Select(OrganizationDtoMapper.Projection())
            .ToListAsync(cancellationToken);

        return new PagedResultDto<OrganizationDto>
        {
            Data = data,
            TotalCount = totalCount
        };
    }
}
