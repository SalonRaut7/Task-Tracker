using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, IReadOnlyList<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public GetProjectsQueryHandler(
        IProjectRepository projectRepository,
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<IReadOnlyList<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _projectRepository.Query();

        if (request.OrganizationId.HasValue)
        {
            query = query.Where(project => project.OrganizationId == request.OrganizationId.Value);
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

            query = query.Where(project => organizationIds.Contains(project.OrganizationId));
        }

        return await query
            .OrderBy(project => project.Name)
            .Select(project => new ProjectDto
            {
                Id = project.Id,
                OrganizationId = project.OrganizationId,
                Name = project.Name,
                Key = project.Key,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
