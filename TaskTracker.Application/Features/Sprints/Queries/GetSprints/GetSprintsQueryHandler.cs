using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Queries.GetSprints;

public sealed class GetSprintsQueryHandler : IRequestHandler<GetSprintsQuery, IReadOnlyList<SprintDto>>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public GetSprintsQueryHandler(
        ISprintRepository sprintRepository,
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _sprintRepository = sprintRepository;
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<IReadOnlyList<SprintDto>> Handle(GetSprintsQuery request, CancellationToken cancellationToken)
    {
        var query = _sprintRepository.Query();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(sprint => sprint.ProjectId == request.ProjectId.Value);
        }

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

            query = query.Where(sprint => organizationIds.Contains(sprint.Project.OrganizationId));
        }

        return await query
            .OrderByDescending(sprint => sprint.CreatedAt)
            .Select(sprint => new SprintDto
            {
                Id = sprint.Id,
                ProjectId = sprint.ProjectId,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status,
                CreatedAt = sprint.CreatedAt,
                UpdatedAt = sprint.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
