using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Invitations;

internal static class InvitationAuthorizationGuard
{
    public static async Task EnsureOrganizationInvitationManagementAsync(
        ScopeType scopeType,
        Guid scopeId,
        string userId,
        bool isSuperAdmin,
        IPermissionEvaluator permissionEvaluator,
        CancellationToken cancellationToken)
    {
        if (scopeType != ScopeType.Organization || isSuperAdmin)
        {
            return;
        }

        var orgRole = await permissionEvaluator.GetUserRoleInScopeAsync(
            userId,
            ScopeType.Organization,
            scopeId,
            cancellationToken);

        if (!string.Equals(orgRole, AppRoles.OrgAdmin, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException("Only OrgAdmin can manage organization invitations.");
        }
    }
}
