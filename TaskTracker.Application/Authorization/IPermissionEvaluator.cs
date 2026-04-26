using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Authorization;

/// <summary>
/// Dynamically evaluates permissions by querying scoped roles from the database.
/// Never reads from JWT claims — always reflects current DB state.
/// </summary>
public interface IPermissionEvaluator
{
    /// <summary>
    /// Checks if a user has a specific permission within a specific scope.
    /// For project scope: checks project role first, falls back to org role.
    /// </summary>
    Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        ScopeType scopeType,
        Guid scopeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user's role in a specific scope (null if not a member).
    /// </summary>
    Task<string?> GetUserRoleInScopeAsync(
        string userId,
        ScopeType scopeType,
        Guid scopeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full scoped permissions map for the /api/me/permissions endpoint.
    /// </summary>
    Task<DTOs.UserPermissionsDto> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
