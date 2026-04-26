using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

/// <summary>
/// Invitation aggregate. Manages onboarding a user into an organization or project scope.
/// Only one active (Pending) invite per user + scope is allowed.
/// </summary>
public class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// Organization or Project.
    public ScopeType ScopeType { get; set; }

    /// The org or project ID this invitation targets.
    public Guid ScopeId { get; set; }

    /// The invited user's email address (normalized to lowercase).
    public string InviteeEmail { get; set; } = string.Empty;

    /// Set if the invitee already has an account at invite-creation time.
    public string? InviteeUserId { get; set; }

    /// The scoped role being granted (e.g. OrgAdmin, Developer).
    public string Role { get; set; } = string.Empty;

    /// SHA-256 hash of the secure invite token. Raw token is never stored.
    public string TokenHash { get; set; } = string.Empty;

    /// When the invitation expires.
    public DateTime ExpiresAt { get; set; }

    /// Current lifecycle status.
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// The user who created this invitation.
    public string InvitedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Navigation
    public ApplicationUser InvitedByUser { get; set; } = null!;

    // ── Domain methods ──────────────────────────────────────

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public void Accept(string userId)
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot accept invitation in '{Status}' status.");

        if (IsExpired)
            throw new InvalidOperationException("This invitation has expired.");

        Status = InvitationStatus.Accepted;
        AcceptedAt = DateTime.UtcNow;
        InviteeUserId = userId;
    }

    public void Revoke()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot revoke invitation in '{Status}' status.");

        Status = InvitationStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
    }

    public void RegenerateToken(string newTokenHash, DateTime newExpiry)
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException($"Cannot resend invitation in '{Status}' status.");

        TokenHash = newTokenHash;
        ExpiresAt = newExpiry;
    }
}
