namespace TaskTracker.Domain.Entities.Identity;

/// <summary>
/// Represents a refresh token stored in the database.
/// Supports token rotation and revocation.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;          
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }               // hash of the replacement token
    public string? CreatedByIp { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;

    // Domain helpers
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByTokenHash;
    }
}
