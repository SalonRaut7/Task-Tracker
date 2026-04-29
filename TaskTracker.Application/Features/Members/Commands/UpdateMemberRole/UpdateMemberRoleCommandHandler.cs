using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Commands.UpdateMemberRole;

public sealed class UpdateMemberRoleCommandHandler : IRequestHandler<UpdateMemberRoleCommand, MemberDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public UpdateMemberRoleCommandHandler(
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<MemberDto> Handle(UpdateMemberRoleCommand request, CancellationToken cancellationToken)
    {
        if (!AppRoles.IsValidForScope(request.NewRole, request.ScopeType))
            throw new InvalidOperationException($"Role '{request.NewRole}' is not valid for scope '{request.ScopeType}'.");

        if (request.ScopeType == ScopeType.Organization &&
            string.Equals(request.NewRole, AppRoles.OrgAdmin, StringComparison.Ordinal) &&
            !_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can assign the OrgAdmin role.");
        }

        if (request.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot change your own role.");

        if (request.ScopeType == ScopeType.Organization)
        {
            var currentRole = await _membershipRepository.GetOrganizationMembershipsAsync(request.ScopeId, cancellationToken);
            var targetRole = currentRole
                .FirstOrDefault(m => m.UserId == request.UserId)?.Role;

            if (string.Equals(targetRole, AppRoles.OrgAdmin, StringComparison.Ordinal) && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenAccessException("Only SuperAdmin can change another OrgAdmin's role.");
            }
        }

        if (request.ScopeType == ScopeType.Organization)
        {
            var membership = await _membershipRepository.UpdateOrganizationMemberRoleAsync(
                request.UserId, request.ScopeId, request.NewRole, cancellationToken);
            return new MemberDto
            {
                UserId = membership.UserId,
                Email = membership.User.Email!,
                FirstName = membership.User.FirstName,
                LastName = membership.User.LastName,
                Role = membership.Role,
                JoinedAt = membership.JoinedAt
            };
        }

        var projectMembership = await _membershipRepository.UpdateProjectMemberRoleAsync(
            request.UserId, request.ScopeId, request.NewRole, cancellationToken);
        return new MemberDto
        {
            UserId = projectMembership.UserId,
            Email = projectMembership.User.Email!,
            FirstName = projectMembership.User.FirstName,
            LastName = projectMembership.User.LastName,
            Role = projectMembership.Role,
            JoinedAt = projectMembership.JoinedAt
        };
    }
}