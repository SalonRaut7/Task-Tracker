using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Queries.GetMembersByScope;

public sealed class GetMembersByScopeQueryHandler : IRequestHandler<GetMembersByScopeQuery, ScopeMembersDto>
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IInvitationRepository _invitationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetMembersByScopeQueryHandler(
        IMembershipRepository membershipRepository,
        IInvitationRepository invitationRepository,
        IProjectRepository projectRepository,
        UserManager<ApplicationUser> userManager)
    {
        _membershipRepository = membershipRepository;
        _invitationRepository = invitationRepository;
        _projectRepository = projectRepository;
        _userManager = userManager;
    }

    public async Task<ScopeMembersDto> Handle(GetMembersByScopeQuery request, CancellationToken cancellationToken)
    {
        List<MemberDto> members;
        List<ApplicationUser> invitableUsers;

        if (request.ScopeType == ScopeType.Organization)
        {
            var memberships = await _membershipRepository.GetOrganizationMembershipsAsync(request.ScopeId, cancellationToken);
            var activeMemberships = memberships
                .Where(uo => uo.User.IsActive && !uo.User.IsArchived)
                .ToList();

            members = activeMemberships.Select(uo => new MemberDto
            {
                UserId = uo.UserId,
                Email = uo.User.Email!,
                FirstName = uo.User.FirstName,
                LastName = uo.User.LastName,
                Role = uo.Role,
                JoinedAt = uo.JoinedAt
            }).ToList();

            var memberUserIds = activeMemberships
                .Select(m => m.UserId)
                .ToHashSet(StringComparer.Ordinal);

            var superAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            var excludedIds = memberUserIds.Concat(superAdmins.Select(u => u.Id)).ToHashSet(StringComparer.Ordinal);

            invitableUsers = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.IsActive && !u.IsArchived && !excludedIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }
        else
        {
            var memberships = await _membershipRepository.GetProjectMembershipsAsync(request.ScopeId, cancellationToken);
            var activeMemberships = memberships
                .Where(up => up.User.IsActive && !up.User.IsArchived)
                .ToList();

            members = activeMemberships.Select(up => new MemberDto
            {
                UserId = up.UserId,
                Email = up.User.Email!,
                FirstName = up.User.FirstName,
                LastName = up.User.LastName,
                Role = up.Role,
                JoinedAt = up.JoinedAt
            }).ToList();

            var project = await _projectRepository.GetByIdAsync(request.ScopeId, cancellationToken)
                ?? throw new InvalidOperationException("Project not found.");

            var organizationMemberships = await _membershipRepository
                .GetOrganizationMembershipsAsync(project.OrganizationId, cancellationToken);

            var projectMemberUserIds = activeMemberships
                .Select(m => m.UserId)
                .ToHashSet(StringComparer.Ordinal);

            var superAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            var superAdminIds = superAdmins.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);

            invitableUsers = organizationMemberships
                .Where(uo => uo.User.IsActive && !uo.User.IsArchived && !projectMemberUserIds.Contains(uo.UserId) && !superAdminIds.Contains(uo.UserId))
                .Select(uo => uo.User)
                .ToList();
        }

        // Override the displayed role for any member who is a global SuperAdmin.
        // This ensures the member list shows "SuperAdmin" instead of whatever
        // scoped role (e.g. OrgAdmin) was stored in the membership record.
        var superAdminUsers = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
        var superAdminIdSet = superAdminUsers.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var member in members)
        {
            if (superAdminIdSet.Contains(member.UserId))
            {
                member.Role = AppRoles.SuperAdmin;
            }
        }

        var allInvitations = await _invitationRepository.GetByScopeAsync(
            request.ScopeType, request.ScopeId, cancellationToken);

        var pendingInvitations = allInvitations
            .Where(i => i.Status == InvitationStatus.Pending && !i.IsExpired)
            .Select(invitation => InvitationDtoMapper.ToDto(invitation))
            .ToList();

        var pendingInvitationEmails = pendingInvitations
            .Select(i => i.InviteeEmail.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var mappedInvitableUsers = invitableUsers
            .Where(u => !string.IsNullOrWhiteSpace(u.Email))
            .Select(u => new InvitableUserDto
            {
                UserId = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = $"{u.FirstName} {u.LastName}".Trim()
            })
            .Where(u => !pendingInvitationEmails.Contains(u.Email.Trim().ToLowerInvariant()))
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .ToList();

        var mentionableUsers = members
            .Select(member => new MentionableUserDto
            {
                UserId = member.UserId,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Role = member.Role
            })
            .Concat(superAdminUsers
                .Where(user => user.IsActive && !user.IsArchived && !members.Any(member => member.UserId == user.Id))
                .Select(user => new MentionableUserDto
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = AppRoles.SuperAdmin
                }))
            .OrderBy(user => $"{user.FirstName} {user.LastName}".Trim())
            .ThenBy(user => user.Role)
            .ToList();

        return new ScopeMembersDto
        {
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            Members = members,
            PendingInvitations = pendingInvitations,
            InvitableUsers = mappedInvitableUsers,
            MentionableUsers = mentionableUsers
        };
    }
}
