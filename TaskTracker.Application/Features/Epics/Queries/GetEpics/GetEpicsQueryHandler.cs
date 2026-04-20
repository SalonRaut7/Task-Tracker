using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Epics.Queries.GetEpics;

public sealed class GetEpicsQueryHandler : IRequestHandler<GetEpicsQuery, IReadOnlyList<EpicDto>>
{
    private readonly IEpicRepository _epicRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserResourceAccessService _resourceAccessService;

    public GetEpicsQueryHandler(
        IEpicRepository epicRepository,
        ICurrentUserService currentUser,
        IUserResourceAccessService resourceAccessService)
    {
        _epicRepository = epicRepository;
        _currentUser = currentUser;
        _resourceAccessService = resourceAccessService;
    }

    public async Task<IReadOnlyList<EpicDto>> Handle(GetEpicsQuery request, CancellationToken cancellationToken)
    {
        var query = _epicRepository.Query();

        if (request.ProjectId.HasValue)
        {
            query = query.Where(epic => epic.ProjectId == request.ProjectId.Value);
        }

        if (!_currentUser.Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Authentication is required.");
            }

            var organizationIds = await _resourceAccessService.GetUserOrganizationIdsAsync(userId, cancellationToken);
            if (organizationIds.Count == 0)
            {
                return [];
            }

            query = query.Where(epic => organizationIds.Contains(epic.Project.OrganizationId));
        }

        return await query
            .OrderByDescending(epic => epic.CreatedAt)
            .Select(epic => new EpicDto
            {
                Id = epic.Id,
                ProjectId = epic.ProjectId,
                Title = epic.Title,
                Description = epic.Description,
                Status = epic.Status,
                CreatedAt = epic.CreatedAt,
                UpdatedAt = epic.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
