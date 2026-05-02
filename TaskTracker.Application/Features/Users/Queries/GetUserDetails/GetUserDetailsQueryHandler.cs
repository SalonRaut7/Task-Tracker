using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Users.Queries.GetUserDetails;

public sealed class GetUserDetailsQueryHandler : IRequestHandler<GetUserDetailsQuery, UserDetailsDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUser;

    public GetUserDetailsQueryHandler(IUserRepository userRepository, ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<UserDetailsDto?> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        EnsureSuperAdmin();

        var user = await _userRepository.GetUserDetailsAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new UserDetailsDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            IsArchived = user.IsArchived,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            ArchivedAtUtc = user.ArchivedAtUtc,
            ArchivedByUserId = user.ArchivedByUserId,
            ArchiveReason = user.ArchiveReason,
            AssignedTaskCount = user.AssignedTaskCount,
            ReportedTaskCount = user.ReportedTaskCount,
            OrganizationMemberships = user.OrganizationMemberships
                .Select(membership => new UserOrganizationSummaryDto
                {
                    OrganizationId = membership.OrganizationId,
                    OrganizationName = membership.OrganizationName,
                    Role = membership.Role,
                    JoinedAt = membership.JoinedAt,
                })
                .ToList(),
            ProjectMemberships = user.ProjectMemberships
                .Select(membership => new UserProjectSummaryDto
                {
                    ProjectId = membership.ProjectId,
                    ProjectName = membership.ProjectName,
                    OrganizationId = membership.OrganizationId,
                    OrganizationName = membership.OrganizationName,
                    Role = membership.Role,
                    JoinedAt = membership.JoinedAt,
                })
                .ToList(),
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
