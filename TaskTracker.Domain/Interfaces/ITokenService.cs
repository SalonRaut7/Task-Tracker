using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Interfaces;

/// Generates and validates JWT access tokens and refresh tokens.
/// JWT is lean: only identity + global role (SuperAdmin). No scoped permissions.
public interface ITokenService
{
    /// Generates a JWT access token with user identity and global roles.
    /// Scoped roles/permissions are NOT included in the token.
    string GenerateAccessToken(ApplicationUser user, IList<string> roles);

    /// Generates a cryptographically random refresh token string.
    string GenerateRefreshToken();

    /// Hashes a token (refresh or invite) for secure storage.
    string HashToken(string token);
}
