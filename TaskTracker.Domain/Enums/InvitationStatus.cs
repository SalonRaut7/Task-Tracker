namespace TaskTracker.Domain.Enums;

/// Tracks the lifecycle state of an invitation.
public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Revoked = 2,
    Expired = 3
}
