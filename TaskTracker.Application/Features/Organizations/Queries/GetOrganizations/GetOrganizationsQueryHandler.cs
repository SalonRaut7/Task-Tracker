using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Organizations.Queries.GetOrganizations;

public sealed class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, IReadOnlyList<OrganizationDto>>
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

    public async Task<IReadOnlyList<OrganizationDto>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var query = _organizationRepository.Query();

        if (!_currentUser.IsSuperAdmin)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Authentication is required.");
            }

            var organizationIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId, cancellationToken);
            if (organizationIds.Count == 0)
            {
                return [];
            }

            query = query.Where(org => organizationIds.Contains(org.Id));
        }

        return await query
            .OrderBy(org => org.Name)
            .Select(org => new OrganizationDto
            {
                Id = org.Id,
                Name = org.Name,
                Slug = org.Slug,
                Description = org.Description,
                CreatedAt = org.CreatedAt,
                UpdatedAt = org.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
