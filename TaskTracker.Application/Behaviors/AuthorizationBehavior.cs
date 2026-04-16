using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Behaviors;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IUserResourceAccessService _resourceAccessService;

    public AuthorizationBehavior(
        ICurrentUserService currentUser,
        IUserResourceAccessService resourceAccessService)
    {
        _currentUser = currentUser;
        _resourceAccessService = resourceAccessService;
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

        var roles = _currentUser.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // SuperAdmin bypasses permission and scope checks by design.
        if (roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            return await next();
        }

        var grantedPermissions = roles
            .SelectMany(AppPermissions.GetPermissionsForRole)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!grantedPermissions.Contains(authorizedRequest.RequiredPermission))
        {
            throw new ForbiddenAccessException(
                $"Missing required permission '{authorizedRequest.RequiredPermission}'.");
        }

        var userId = _currentUser.UserId!;
        var scopes = authorizedRequest.Scopes ?? [];

        foreach (var scope in scopes)
        {
            var canAccessScope = scope.ResourceType switch
            {
                ResourceType.Organization => await _resourceAccessService.CanAccessOrganizationAsync(userId, scope.Id, cancellationToken),
                ResourceType.Project => await _resourceAccessService.CanAccessProjectAsync(userId, scope.Id, cancellationToken),
                ResourceType.Comment => await _resourceAccessService.CanAccessCommentAsync(userId, scope.Id, cancellationToken),
                ResourceType.Epic => await _resourceAccessService.CanAccessEpicAsync(userId, scope.Id, cancellationToken),
                ResourceType.Sprint => await _resourceAccessService.CanAccessSprintAsync(userId, scope.Id, cancellationToken),
                _ => false
            };

            if (!canAccessScope)
            {
                throw new ForbiddenAccessException(
                    $"No access to {scope.ResourceType} resource '{scope.Id}'.");
            }
        }

        return await next();
    }
}