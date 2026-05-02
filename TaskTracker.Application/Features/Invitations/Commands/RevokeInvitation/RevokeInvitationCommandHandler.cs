using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Features.Invitations;
using TaskTracker.Application.Interfaces;
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
        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new InvalidOperationException("Invitation not found.");

        var userId = _currentUser.RequireUserId();

        await InvitationAuthorizationGuard.EnsureOrganizationInvitationManagementAsync(
            invitation.ScopeType,
            invitation.ScopeId,
            userId,
            _currentUser.IsSuperAdmin,
            _permissionEvaluator,
            cancellationToken);

        invitation.Revoke();
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        await _pushService.BroadcastScopeMembersChangedAsync(invitation.ScopeType, invitation.ScopeId, cancellationToken);
        return true;
    }
}
