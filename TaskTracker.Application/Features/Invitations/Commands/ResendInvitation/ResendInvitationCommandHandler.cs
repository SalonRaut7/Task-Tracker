using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Options;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Invitations.Commands.ResendInvitation;

public sealed class ResendInvitationCommandHandler : IRequestHandler<ResendInvitationCommand, InvitationDto>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly INotificationPushService _pushService;
    private readonly InviteOptions _inviteOptions;

    public ResendInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        ITokenService tokenService,
        IEmailSender emailSender,
        IOrganizationRepository organizationRepository,
        IProjectRepository projectRepository,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator,
        INotificationPushService pushService,
        IOptions<InviteOptions> inviteOptions)
    {
        _invitationRepository = invitationRepository;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _organizationRepository = organizationRepository;
        _projectRepository = projectRepository;
        _userManager = userManager;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
        _pushService = pushService;
        _inviteOptions = inviteOptions.Value;
    }

    public async Task<InvitationDto> Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required.");

        var invitation = await _invitationRepository.GetByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new InvalidOperationException("Invitation not found.");

        var userId = _currentUser.UserId!;

        var canResend = await _permissionEvaluator.HasPermissionAsync(
            userId,
            AppPermissions.InvitationsCreate,
            invitation.ScopeType,
            invitation.ScopeId,
            cancellationToken);

        if (!canResend)
            throw new ForbiddenAccessException("You do not have permission to resend invitations in this scope.");

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

        var rawToken = GenerateSecureToken();
        var tokenHash = _tokenService.HashToken(rawToken);
        var newExpiry = DateTime.UtcNow.AddDays(_inviteOptions.ExpirationDays);

        invitation.RegenerateToken(tokenHash, newExpiry);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        string scopeName = invitation.ScopeType == Domain.Enums.ScopeType.Organization
            ? (await _organizationRepository.GetByIdAsync(invitation.ScopeId, cancellationToken))?.Name ?? "Organization"
            : (await _projectRepository.GetByIdAsync(invitation.ScopeId, cancellationToken))?.Name ?? "Project";

        var inviterUser = await _userManager.FindByIdAsync(_currentUser.UserId!);
        var inviterName = inviterUser is not null
            ? $"{inviterUser.FirstName} {inviterUser.LastName}".Trim()
            : "A team member";

        var inviteLink = $"{_inviteOptions.AcceptUrl}?token={Uri.EscapeDataString(rawToken)}";

        await _emailSender.SendInviteAsync(
            invitation.InviteeEmail, inviterName, scopeName, invitation.Role, inviteLink, cancellationToken);

        await _pushService.BroadcastScopeMembersChangedAsync(invitation.ScopeType, invitation.ScopeId, cancellationToken);

        return new InvitationDto
        {
            Id = invitation.Id,
            ScopeType = invitation.ScopeType,
            ScopeId = invitation.ScopeId,
            InviteeEmail = invitation.InviteeEmail,
            Role = invitation.Role,
            Status = invitation.Status,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedByName = inviterName,
            CreatedAt = invitation.CreatedAt,
            ExpiresAt = invitation.ExpiresAt
        };
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}