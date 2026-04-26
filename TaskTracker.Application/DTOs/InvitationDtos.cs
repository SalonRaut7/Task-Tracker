using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs;

public sealed class InvitationDto
{
    public Guid Id { get; init; }
    public ScopeType ScopeType { get; init; }
    public Guid ScopeId { get; init; }
    public string InviteeEmail { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public InvitationStatus Status { get; init; }
    public string InvitedByUserId { get; init; } = string.Empty;
    public string InvitedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
}

public sealed class CreateInvitationDto
{
    public ScopeType ScopeType { get; set; }
    public Guid ScopeId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class AcceptInvitationDto
{
    public string Token { get; set; } = string.Empty;
}
