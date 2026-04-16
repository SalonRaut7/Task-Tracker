using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Domain.Entities.Identity;
/// Extended Identity user with application-specific properties.
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties: Means this user can belong to an organization and have multiple refresh tokens and OTP entries.
    public Organization? Organization { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<OtpEntry> OtpEntries { get; set; } = new List<OtpEntry>();
}
