using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Queries.GetArchivedUsers;

public sealed class GetArchivedUsersQueryHandler : IRequestHandler<GetArchivedUsersQuery, PagedResultDto<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetArchivedUsersQueryHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<UserSummaryDto>> Handle(GetArchivedUsersQuery request, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin();

        var summaryResult = await _userRepository.GetUserSummariesAsync(
            archived: true,
            search: request.Search,
            skip: request.Skip,
            take: request.Take,
            cancellationToken: cancellationToken);

        var data = summaryResult.Users
            .Select(user => new UserSummaryDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsSuperAdmin = user.IsSuperAdmin,
                IsActive = user.IsActive,
                IsArchived = user.IsArchived,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                ArchivedAtUtc = user.ArchivedAtUtc,
                ArchivedByUserId = user.ArchivedByUserId,
                ArchiveReason = user.ArchiveReason,
                OrganizationCount = user.OrganizationCount,
                ProjectCount = user.ProjectCount,
                AssignedTaskCount = user.AssignedTaskCount,
                ReportedTaskCount = user.ReportedTaskCount,
            })
            .ToList();

        return new PagedResultDto<UserSummaryDto>
        {
            Data = data,
            TotalCount = summaryResult.TotalCount
        };
    }

    private void EnsureSuperAdmin()
    {
        if (!_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can access user administration.");
        }
    }
}
