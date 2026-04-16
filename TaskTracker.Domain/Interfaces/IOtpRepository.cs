using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Interfaces;
/// Abstracts OTP entry persistence operations.

public interface IOtpRepository
{
    /// <summary>Adds a new OTP entry to storage.</summary>
    Task<OtpEntry> AddAsync(OtpEntry otpEntry, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing OTP entry.</summary>
    Task<OtpEntry> UpdateAsync(OtpEntry otpEntry, CancellationToken cancellationToken = default);

    /// <summary>Gets the latest active (unused and not expired) OTP for a user by purpose.</summary>
    Task<OtpEntry?> GetLatestActiveByUserIdAndPurposeAsync(
        string userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default);

    /// <summary>Gets all OTP entries for a user with a specific purpose.</summary>
    Task<IEnumerable<OtpEntry>> GetByUserIdAndPurposeAsync(
        string userId,
        OtpPurpose purpose,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an OTP entry.</summary>
    Task DeleteAsync(Guid otpId, CancellationToken cancellationToken = default);
}
