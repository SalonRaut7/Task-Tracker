using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Options;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Invitations.Commands.CreateInvitation;

public sealed class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, InvitationDto>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly INotificationPushService _pushService;
    private readonly InviteOptions _inviteOptions;

    public CreateInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        IOrganizationRepository organizationRepository,
        IProjectRepository projectRepository,
        ICurrentUserService currentUser,
        ITokenService tokenService,
        IEmailSender emailSender,
        UserManager<ApplicationUser> userManager,
        IPermissionEvaluator permissionEvaluator,
        INotificationPushService pushService,
        IOptions<InviteOptions> inviteOptions)
    {
        _invitationRepository = invitationRepository;
        _organizationRepository = organizationRepository;
        _projectRepository = projectRepository;
        _currentUser = currentUser;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _userManager = userManager;
        _permissionEvaluator = permissionEvaluator;
        _pushService = pushService;
        _inviteOptions = inviteOptions.Value;
    }

    public async Task<InvitationDto> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
            throw new UnauthorizedAccessException("Authentication is required.");

        var normalizedEmail = request.InviteeEmail.Trim().ToLowerInvariant();
        var inviterUserId = _currentUser.UserId!;

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is null)
            throw new InvalidOperationException("The invited email address is not registered.");

        if (request.ScopeType == ScopeType.Organization &&
            string.Equals(request.Role, AppRoles.OrgAdmin, StringComparison.Ordinal) &&
            !_currentUser.IsSuperAdmin)
        {
            throw new ForbiddenAccessException("Only SuperAdmin can invite users as OrgAdmin.");
        }

        string scopeName;
        Guid? parentOrgId = null;

        if (request.ScopeType == ScopeType.Organization)
        {
            var org = await _organizationRepository.GetByIdAsync(request.ScopeId, cancellationToken)
                ?? throw new InvalidOperationException("Organization not found.");
            scopeName = org.Name;
        }
        else
        {
            var project = await _projectRepository.GetByIdAsync(request.ScopeId, cancellationToken)
                ?? throw new InvalidOperationException("Project not found.");
            scopeName = project.Name;
            parentOrgId = project.OrganizationId;
        }

        var inviterRoleInScope = await _permissionEvaluator.GetUserRoleInScopeAsync(
            inviterUserId, request.ScopeType, request.ScopeId, cancellationToken);

        if (inviterRoleInScope is null && request.ScopeType == ScopeType.Project && parentOrgId.HasValue)
        {
            inviterRoleInScope = await _permissionEvaluator.GetUserRoleInScopeAsync(
                inviterUserId, ScopeType.Organization, parentOrgId.Value, cancellationToken);
        }

        var assignableRoles = GetAssignableRoles(
            request.ScopeType,
            inviterRoleInScope,
            _currentUser.IsSuperAdmin);

        if (assignableRoles.Count == 0)
        {
            throw new ForbiddenAccessException(
                "You are not allowed to invite members in this scope.");
        }

        if (!assignableRoles.Contains(request.Role, StringComparer.Ordinal))
        {
            throw new ForbiddenAccessException(
                $"You are not allowed to assign the '{request.Role}' role in this scope.");
        }

        var existingInvite = await _invitationRepository.GetActiveByScopeAndEmailAsync(
            request.ScopeType, request.ScopeId, normalizedEmail, cancellationToken);

        if (existingInvite is not null)
            throw new InvalidOperationException(
                "An active invitation already exists for this user in this scope.");

        var rawToken = GenerateSecureToken();
        var tokenHash = _tokenService.HashToken(rawToken);

        var invitation = new Invitation
        {
            ScopeType = request.ScopeType,
            ScopeId = request.ScopeId,
            InviteeEmail = normalizedEmail,
            InviteeUserId = existingUser?.Id,
            Role = request.Role,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_inviteOptions.ExpirationDays),
            Status = InvitationStatus.Pending,
            InvitedByUserId = inviterUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        var inviterUser = await _userManager.FindByIdAsync(inviterUserId);
        var inviterName = inviterUser is not null
            ? $"{inviterUser.FirstName} {inviterUser.LastName}".Trim()
            : "A team member";

        var inviteLink = $"{_inviteOptions.AcceptUrl}?token={Uri.EscapeDataString(rawToken)}";

        await _emailSender.SendInviteAsync(
            normalizedEmail, inviterName, scopeName, request.Role, inviteLink, cancellationToken);

        await _pushService.BroadcastScopeMembersChangedAsync(request.ScopeType, request.ScopeId, cancellationToken);

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

    private static IReadOnlyList<string> GetAssignableRoles(
        ScopeType scopeType,
        string? inviterRole,
        bool isSuperAdmin)
    {
        if (isSuperAdmin)
        {
            return scopeType == ScopeType.Organization
                ? AppRoles.OrganizationRoles
                : AppRoles.ProjectRoles;
        }

        return scopeType switch
        {
            ScopeType.Organization when string.Equals(inviterRole, AppRoles.OrgAdmin, StringComparison.Ordinal)
                => AppRoles.OrganizationRoles,

            ScopeType.Project when string.Equals(inviterRole, AppRoles.OrgAdmin, StringComparison.Ordinal)
                => AppRoles.ProjectRoles,

            ScopeType.Project when string.Equals(inviterRole, AppRoles.ProjectManager, StringComparison.Ordinal)
                => [AppRoles.Developer, AppRoles.QA, AppRoles.Viewer],

            _ => []
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