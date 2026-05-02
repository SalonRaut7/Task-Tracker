using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces:
/// - authentication for IAuthenticatedRequest
/// - permission + scope checks for IAuthorizedRequest
/// Uses IPermissionEvaluator for dynamic, database-backed permission checks.
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IAuthorizationScopeResolver _scopeResolver;

    public AuthorizationBehavior(
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator,
        IAuthorizationScopeResolver scopeResolver)
    {
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
        _scopeResolver = scopeResolver;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var authorizedRequest = request as IAuthorizedRequest;
        var requiresAuthentication = request is IAuthenticatedRequest || authorizedRequest is not null;

        if (!requiresAuthentication)
        {
            return await next();
        }

        var userId = _currentUser.RequireUserId();

        if (authorizedRequest is null)
        {
            return await next();
        }

        // SuperAdmin bypasses all permission and scope checks
        if (_currentUser.IsSuperAdmin)
        {
            return await next();
        }

        var scopes = authorizedRequest.Scopes ?? [];
        var permission = authorizedRequest.RequiredPermission;

        if (scopes.Count == 0)
        {
            // No scope specified — check if user has this permission in ANY scope.
            // This happens for global operations like Organizations.Create where
            // the user just needs to be a SuperAdmin or have the permission
            // through some role. For non-SuperAdmin, we need at least one org
            // membership that grants this permission.
            var userPermissions = await _permissionEvaluator.GetUserPermissionsAsync(userId, cancellationToken);

            var hasPermissionInAnyOrg = userPermissions.OrganizationRoles
                .Any(r => r.Permissions.Contains(permission));
            var hasPermissionInAnyProject = userPermissions.ProjectRoles
                .Any(r => r.Permissions.Contains(permission));

            if (!hasPermissionInAnyOrg && !hasPermissionInAnyProject)
            {
                throw new ForbiddenAccessException(
                    $"Missing required permission '{permission}'.");
            }
        }
        else
        {
            // Check permission in each specified scope
            foreach (var scope in scopes)
            {
                var (scopeType, scopeId) = await _scopeResolver.ResolveScopeAsync(scope, cancellationToken);

                if (scopeId == Guid.Empty)
                {
                    throw new ForbiddenAccessException(
                        $"No access to {scope.ResourceType} resource '{scope.Id}' " +
                        $"for permission '{permission}'.");
                }

                var canAccess = await _permissionEvaluator.HasPermissionAsync(
                    userId, permission, scopeType, scopeId, cancellationToken);

                if (!canAccess)
                {
                    throw new ForbiddenAccessException(
                        $"No access to {scope.ResourceType} resource '{scope.Id}' " +
                        $"for permission '{permission}'.");
                }
            }
        }

        return await next();
    }
}
