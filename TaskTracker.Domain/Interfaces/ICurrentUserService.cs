namespace TaskTracker.Domain.Interfaces;

/// Provides access to the currently authenticated user's identity information.

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
}
