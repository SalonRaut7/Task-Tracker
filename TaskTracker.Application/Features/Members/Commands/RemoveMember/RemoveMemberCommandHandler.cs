using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Constants;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Members.Commands.RemoveMember;

public sealed class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, bool>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;
    private readonly INotificationPushService _pushService;
    private readonly ICacheService _cache;

    public RemoveMemberCommandHandler(
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository,
        INotificationPushService pushService,
        ICacheService cache)
    {
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
        _pushService = pushService;
        _cache = cache;
    }

    public async Task<bool> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == _currentUser.UserId)
            throw new InvalidOperationException("You cannot remove yourself from a scope.");

        if (request.ScopeType == ScopeType.Organization)
        {
            var memberships = await _membershipRepository.GetOrganizationMembershipsAsync(request.ScopeId, cancellationToken);
            var targetRole = memberships.FirstOrDefault(m => m.UserId == request.UserId)?.Role;

            if (string.Equals(targetRole, AppRoles.OrgAdmin, StringComparison.Ordinal) && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenAccessException("Only SuperAdmin can remove another OrgAdmin.");
            }
        }

        if (request.ScopeType == ScopeType.Organization)
        {
            await _membershipRepository.RemoveOrganizationMemberAsync(
                request.UserId, request.ScopeId, cancellationToken);
        }
        else
        {
            await _membershipRepository.RemoveProjectMemberAsync(
                request.UserId, request.ScopeId, cancellationToken);
        }

        await _pushService.BroadcastScopeMembersChangedAsync(request.ScopeType, request.ScopeId, cancellationToken);
        await _pushService.BroadcastUserWorkspaceChangedAsync(request.UserId, cancellationToken);

        // Invalidate the removed user's membership and permissions cache.
        // MembershipRepository.Remove already handles the specific key removal,
        // but we also clear the permissions bundle here for belt-and-suspenders.
        _cache.Remove(CacheKeys.UserPermissions(request.UserId));
        _cache.Remove(CacheKeys.UserOrgIds(request.UserId));
        _cache.Remove(CacheKeys.UserProjectIds(request.UserId));

        return true;
    }
}