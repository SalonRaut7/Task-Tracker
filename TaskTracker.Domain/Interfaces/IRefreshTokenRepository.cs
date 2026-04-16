using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Interfaces;
/// Abstracts refresh token persistence operations.
/// Maintains CQRS/Clean Architecture by isolating data access.
public interface IRefreshTokenRepository
{
    ///Adds a new refresh token to storage.
    Task<RefreshToken> AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    ///Finds a refresh token by its hash.
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    ///Finds a refresh token by its hash and user ID.
    Task<RefreshToken?> GetByTokenHashAndUserIdAsync(string tokenHash, string userId, CancellationToken cancellationToken = default);

    ///Updates an existing refresh token (e.g., to revoke it).
    Task<RefreshToken> UpdateAsync(RefreshToken token, CancellationToken cancellationToken = default);

    ///Gets all active (non-revoked, non-expired) refresh tokens for a user.
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    ///Retrieves all refresh tokens for a user (active and inactive).
    Task<IEnumerable<RefreshToken>> GetAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}
