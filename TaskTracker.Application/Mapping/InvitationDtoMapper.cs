using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Mapping;

internal static class InvitationDtoMapper
{
    public static InvitationDto ToDto(Invitation invitation, string? invitedByName = null)
    {
        return new InvitationDto
        {
            Id = invitation.Id,
            ScopeType = invitation.ScopeType,
            ScopeId = invitation.ScopeId,
            InviteeEmail = invitation.InviteeEmail,
            Role = invitation.Role,
            Status = invitation.IsExpired && invitation.Status == InvitationStatus.Pending
                ? InvitationStatus.Expired
                : invitation.Status,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedByName = ResolveInvitedByName(invitation, invitedByName),
            CreatedAt = invitation.CreatedAt,
            ExpiresAt = invitation.ExpiresAt,
            AcceptedAt = invitation.AcceptedAt,
            RevokedAt = invitation.RevokedAt
        };
    }

    private static string ResolveInvitedByName(Invitation invitation, string? invitedByName)
    {
        if (!string.IsNullOrWhiteSpace(invitedByName))
        {
            return invitedByName;
        }

        if (invitation.InvitedByUser is null)
        {
            return string.Empty;
        }

        return $"{invitation.InvitedByUser.FirstName} {invitation.InvitedByUser.LastName}".Trim();
    }
}
