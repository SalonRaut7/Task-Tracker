using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Invitations.Commands.RevokeInvitation;

public sealed class RevokeInvitationCommandHandler : IRequestHandler<RevokeInvitationCommand, bool>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly INotificationPushService _pushService;

    public RevokeInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator,
        INotificationPushService pushService)
    {
        _invitationRepository = invitationRepository;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
        _pushService = pushService;
    }

    public async Task<bool> Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required.");

        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new InvalidOperationException("Invitation not found.");

        var userId = _currentUser.UserId!;

        var canRevoke = await _permissionEvaluator.HasPermissionAsync(
            userId,
            AppPermissions.InvitationsRevoke,
            invitation.ScopeType,
            invitation.ScopeId,
            cancellationToken);

        if (!canRevoke)
            throw new ForbiddenAccessException("You do not have permission to revoke invitations in this scope.");

        if (invitation.ScopeType == ScopeType.Organization)
        {
            var orgRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                userId,
                ScopeType.Organization,
                invitation.ScopeId,
                cancellationToken);

            if (!_currentUser.IsSuperAdmin && !string.Equals(orgRole, AppRoles.OrgAdmin, StringComparison.Ordinal))
                throw new ForbiddenAccessException("Only OrgAdmin can manage organization invitations.");
        }

        invitation.Revoke();
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        await _pushService.BroadcastScopeMembersChangedAsync(invitation.ScopeType, invitation.ScopeId, cancellationToken);
        return true;
    }
}