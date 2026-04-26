using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that enforces authorization on commands/queries
/// implementing IAuthorizedRequest. Uses IPermissionEvaluator for dynamic,
/// database-backed permission checks (not JWT claims).
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ICommentRepository? _commentRepository;
    private readonly ITaskRepository? _taskRepository;

    public AuthorizationBehavior(
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator,
        ICommentRepository? commentRepository = null,
        ITaskRepository? taskRepository = null)
    {
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedRequest authorizedRequest)
        {
            return await next();
        }

        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        // SuperAdmin bypasses all permission and scope checks
        if (_currentUser.IsSuperAdmin)
        {
            return await next();
        }

        var userId = _currentUser.UserId!;
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
                var (scopeType, scopeId) = await ResolveAuthorizationScopeAsync(scope, cancellationToken);

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

    private async Task<(ScopeType scopeType, Guid scopeId)> ResolveAuthorizationScopeAsync(
        ResourceScope scope,
        CancellationToken cancellationToken)
    {
        if (scope.ResourceType == ResourceType.Organization)
        {
            return (ScopeType.Organization, scope.Id);
        }

        if (scope.ResourceType == ResourceType.Project)
        {
            return (ScopeType.Project, scope.Id);
        }

        if (scope.ResourceType == ResourceType.Comment)
        {
            if (_commentRepository is null || _taskRepository is null)
            {
                return (ScopeType.Project, Guid.Empty);
            }

            var comment = await _commentRepository.GetByIdAsync(scope.Id, cancellationToken);
            if (comment is null)
            {
                return (ScopeType.Project, Guid.Empty);
            }

            var task = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken);
            if (task is null)
            {
                return (ScopeType.Project, Guid.Empty);
            }

            return (ScopeType.Project, task.ProjectId);
        }

        // Other child resources currently use project-scoped authorization.
        return (ScopeType.Project, scope.Id);
    }
}