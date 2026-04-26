using MediatR;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Invitations.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommand : IRequest<AcceptInvitationResult>
{
    public string Token { get; set; } = string.Empty;
}

public sealed class AcceptInvitationResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public ScopeType? ScopeType { get; init; }
    public Guid? ScopeId { get; init; }
    public string? Role { get; init; }
}

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

        // 1. Hash token and look it up
        var tokenHash = _tokenService.HashToken(request.Token);
        var invitation = await _invitationRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (invitation is null)
            return new AcceptInvitationResult { Success = false, Message = "Invalid invitation token." };

        // 2. Validate email matches
        var currentEmail = _currentUser.Email?.ToLowerInvariant();
        if (!string.Equals(invitation.InviteeEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
            return new AcceptInvitationResult { Success = false, Message = "This invitation was sent to a different email address." };

        var userId = _currentUser.UserId!;

        // 3. Check status and expiry
        if (invitation.Status == InvitationStatus.Accepted)
        {
            // Idempotent accept for the same user prevents duplicate-call 400s.
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

        // 4. Accept the invitation (domain logic validates status + expiry)
        invitation.Accept(userId);

        // 5. Create or update membership (accept replaces existing role)
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

        // 6. Persist invitation state
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
