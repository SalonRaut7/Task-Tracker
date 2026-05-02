using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, PagedResultDto<ProjectDto>>
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

    public async Task<PagedResultDto<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _projectRepository.Query();

        if (request.OrganizationId.HasValue)
        {
            query = query.Where(project => project.OrganizationId == request.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();
            var likePattern = $"%{searchTerm.Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(project =>
                EF.Functions.ILike(project.Name, likePattern) ||
                EF.Functions.ILike(project.Key, likePattern));
        }

        if (!_currentUser.IsSuperAdmin)
        {
            var userId = _currentUser.UserId!;
            var organizationIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId, cancellationToken);
            if (organizationIds.Count == 0)
            {
                return new PagedResultDto<ProjectDto>
                {
                    Data = [],
                    TotalCount = 0
                };
            }

            query = query.Where(project => organizationIds.Contains(project.OrganizationId));
        }

        query = query.OrderBy(project => project.Name);
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
            .Select(ProjectDtoMapper.Projection())
            .ToListAsync(cancellationToken);

        return new PagedResultDto<ProjectDto>
        {
            Data = data,
            TotalCount = totalCount
        };
    }
}
