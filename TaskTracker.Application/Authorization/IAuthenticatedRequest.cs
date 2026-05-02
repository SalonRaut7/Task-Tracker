namespace TaskTracker.Application.Authorization;

/// Marker for requests that require an authenticated user but do not
/// require permission-scoped authorization checks.
public interface IAuthenticatedRequest
{
}
