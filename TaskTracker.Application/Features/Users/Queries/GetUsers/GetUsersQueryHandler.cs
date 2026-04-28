using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Queries.GetUsers;

public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetUsersQueryHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin();

        var users = await _userRepository.GetUserSummariesAsync(archived: false, cancellationToken);

        return users
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
    }

    private void EnsureSuperAdmin()
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        if (!_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can access user administration.");
        }
    }
}
