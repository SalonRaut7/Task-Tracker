using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Interfaces;
/// Generates and validates JWT access tokens and refresh tokens.
public interface ITokenService
{
    /// Generates a JWT access token with user claims, roles, and permissions.
    string GenerateAccessToken(ApplicationUser user, IList<string> roles, IList<string> permissions);

    /// Generates a cryptographically random refresh token string.
    string GenerateRefreshToken();

    /// Hashes a refresh token for secure storage.
    string HashToken(string token);
}
