using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.API.Authorization;

/// <summary>
/// ASP.NET Core authorization handler for permission-based policies.
/// Uses IPermissionEvaluator for dynamic DB-backed checks instead of JWT claims.
/// Note: Most authorization goes through the MediatR AuthorizationBehavior pipeline.
/// This handler covers [Authorize(Policy = "...")] attributes on controllers.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public PermissionAuthorizationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        // SuperAdmin bypasses all permission checks
        if (context.User.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        // Use IPermissionEvaluator to check if user has the permission in any scope
        using var scope = _serviceProvider.CreateScope();
        var evaluator = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();
        var permissions = await evaluator.GetUserPermissionsAsync(userId);

        var hasInAnyOrg = permissions.OrganizationRoles
            .Any(r => r.Permissions.Contains(requirement.Permission));
        var hasInAnyProject = permissions.ProjectRoles
            .Any(r => r.Permissions.Contains(requirement.Permission));

        if (hasInAnyOrg || hasInAnyProject)
        {
            context.Succeed(requirement);
        }
    }
}
