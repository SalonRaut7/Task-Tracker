using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Commands.UpdateMemberRole;

public sealed class UpdateMemberRoleCommand : IRequest<MemberDto>, IAuthorizedRequest
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string NewRole { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.MembersUpdateRole;
    public IReadOnlyList<ResourceScope> Scopes => ScopeType switch
    {
        ScopeType.Organization => [new ResourceScope(ResourceType.Organization, ScopeId)],
        ScopeType.Project => [new ResourceScope(ResourceType.Project, ScopeId)],
        _ => []
    };
}

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
        // Validate role for scope
        if (!AppRoles.IsValidForScope(request.NewRole, request.ScopeType))
            throw new InvalidOperationException($"Role '{request.NewRole}' is not valid for scope '{request.ScopeType}'.");

        // Only SuperAdmin can promote/demote OrgAdmin in organization scope.
        if (request.ScopeType == ScopeType.Organization &&
            string.Equals(request.NewRole, AppRoles.OrgAdmin, StringComparison.Ordinal) &&
            !_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can assign the OrgAdmin role.");
        }

        // Cannot change own role
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
        else
        {
            var membership = await _membershipRepository.UpdateProjectMemberRoleAsync(
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
    }
}
