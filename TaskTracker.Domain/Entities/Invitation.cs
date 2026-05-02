using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

/// <summary>
/// Invitation aggregate. Manages onboarding a user into an organization or project scope.
/// Only one active (Pending) invite per user + scope is allowed.
/// </summary>
public class Invitation
{
    public Guid Id { get; private set; }

    /// Organization or Project.
    public ScopeType ScopeType { get; private set; }

    /// The org or project ID this invitation targets.
    public Guid ScopeId { get; private set; }

    /// The invited user's email address (normalized to lowercase).
    public string InviteeEmail { get; private set; } = string.Empty;

    /// Set if the invitee already has an account at invite-creation time.
    public string? InviteeUserId { get; private set; }

    /// The scoped role being granted (e.g. OrgAdmin, Developer).
    public string Role { get; private set; } = string.Empty;

    /// SHA-256 hash of the secure invite token. Raw token is never stored.
    public string TokenHash { get; private set; } = string.Empty;

    /// When the invitation expires.
    public DateTime ExpiresAt { get; private set; }

    /// Current lifecycle status.
    public InvitationStatus Status { get; private set; } = InvitationStatus.Pending;

    /// The user who created this invitation.
    public string InvitedByUserId { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Navigation
    public ApplicationUser InvitedByUser { get; set; } = null!;

    // Required by EF Core
    private Invitation() { }

    public static Invitation Create(
        ScopeType scopeType,
        Guid scopeId,
        string inviteeEmail,
        string? inviteeUserId,
        string role,
        string tokenHash,
        DateTime expiresAt,
        string invitedByUserId,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(inviteeEmail))
            throw new InvalidOperationException("Invitee email is required.");

        if (string.IsNullOrWhiteSpace(role))
            throw new InvalidOperationException("Role is required.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new InvalidOperationException("Token hash is required.");

        if (string.IsNullOrWhiteSpace(invitedByUserId))
            throw new InvalidOperationException("Inviter user id is required.");

        return new Invitation
        {
            Id = Guid.NewGuid(),
            ScopeType = scopeType,
            ScopeId = scopeId,
            InviteeEmail = inviteeEmail.Trim().ToLowerInvariant(),
            InviteeUserId = string.IsNullOrWhiteSpace(inviteeUserId) ? null : inviteeUserId.Trim(),
            Role = role.Trim(),
            TokenHash = tokenHash,
            ExpiresAt = NormalizeUtc(expiresAt),
            Status = InvitationStatus.Pending,
            InvitedByUserId = invitedByUserId.Trim(),
            CreatedAt = NormalizeUtc(createdAtUtc)
        };
    }

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

        if (string.IsNullOrWhiteSpace(newTokenHash))
            throw new InvalidOperationException("Token hash is required.");

        TokenHash = newTokenHash;
        ExpiresAt = NormalizeUtc(newExpiry);
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
