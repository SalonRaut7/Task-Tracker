using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities.Identity;

/// Stores OTP codes for email verification and password reset.
/// Includes attempt tracking, cooldown, and expiry logic.
public class OtpEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;    
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int AttemptCount { get; set; }
    public bool IsUsed { get; set; }
    public int ResendCount { get; set; }
    public DateTime? LastResentAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;

    // ── Domain logic ─────────────────────────────────────────

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsUsed && !IsExpired && AttemptCount < 5;

    public bool CanResend(int maxResends, int cooldownSeconds)
    {
        if (ResendCount >= maxResends) return false;
        if (LastResentAt.HasValue &&
            (DateTime.UtcNow - LastResentAt.Value).TotalSeconds < cooldownSeconds)
            return false;
        return true;
    }

    public void IncrementAttempt() => AttemptCount++;

    public void MarkUsed() => IsUsed = true;

    public void RecordResend(string newCodeHash, DateTime newExpiry)
    {
        CodeHash = newCodeHash;
        ExpiresAt = newExpiry;
        AttemptCount = 0;
        IsUsed = false;
        ResendCount++;
        LastResentAt = DateTime.UtcNow;
    }
}
