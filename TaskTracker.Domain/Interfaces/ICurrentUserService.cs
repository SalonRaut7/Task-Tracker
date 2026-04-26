namespace TaskTracker.Domain.Interfaces;

/// Provides access to the currently authenticated user's identity information.
/// Scoped permissions are NOT stored here — use IPermissionEvaluator for that.
public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }

    /// True if the user holds the global SuperAdmin role (stored in JWT).
    bool IsSuperAdmin { get; }

    /// Global roles from JWT claims (typically only SuperAdmin or empty).
    IEnumerable<string> Roles { get; }
}
