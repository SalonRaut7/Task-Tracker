using MediatR;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Invitations.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, AcceptInvitationResult>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokenService;
    private readonly IMembershipRepository _membershipRepository;

    public AcceptInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        ICurrentUserService currentUser,
        ITokenService tokenService,
        IMembershipRepository membershipRepository)
    {
        _invitationRepository = invitationRepository;
        _currentUser = currentUser;
        _tokenService = tokenService;
        _membershipRepository = membershipRepository;
    }

    public async Task<AcceptInvitationResult> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("You must be logged in to accept an invitation.");

        var tokenHash = _tokenService.HashToken(request.Token);
        var invitation = await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (invitation is null)
            return new AcceptInvitationResult { Success = false, Message = "Invalid invitation token." };

        var currentEmail = _currentUser.Email?.ToLowerInvariant();
        if (!string.Equals(invitation.InviteeEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            return new AcceptInvitationResult { Success = false, Message = "This invitation was sent to a different email address." };

        var userId = _currentUser.UserId!;

        if (invitation.Status == InvitationStatus.Accepted)
        {
            if (string.Equals(invitation.InviteeUserId, userId, StringComparison.Ordinal))
            {
                return new AcceptInvitationResult
                {
                    Success = true,
                    Message = $"Invitation already accepted as {invitation.Role}.",
                    ScopeType = invitation.ScopeType,
                    ScopeId = invitation.ScopeId,
                    Role = invitation.Role
                };
            }

            return new AcceptInvitationResult { Success = false, Message = "This invitation has already been accepted by another user." };
        }

        if (invitation.Status != InvitationStatus.Pending)
            return new AcceptInvitationResult { Success = false, Message = $"This invitation has already been {invitation.Status.ToString().ToLowerInvariant()}." };

        if (invitation.IsExpired)
            return new AcceptInvitationResult { Success = false, Message = "This invitation has expired. Please ask for a new one." };

        invitation.Accept(userId);

        if (invitation.ScopeType == Domain.Enums.ScopeType.Organization)
        {
            await _membershipRepository.UpsertOrganizationMemberAsync(
                userId, invitation.ScopeId, invitation.Role, invitation.InvitedByUserId, cancellationToken);
        }
        else
        {
            await _membershipRepository.UpsertProjectMemberAsync(
                userId, invitation.ScopeId, invitation.Role, invitation.InvitedByUserId, cancellationToken);
        }

        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        return new AcceptInvitationResult
        {
            Success = true,
            Message = $"You have been added as {invitation.Role}.",
            ScopeType = invitation.ScopeType,
            ScopeId = invitation.ScopeId,
            Role = invitation.Role
        };
    }
}