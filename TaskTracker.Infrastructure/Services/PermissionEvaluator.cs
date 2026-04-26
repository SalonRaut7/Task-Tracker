using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Services;

/// <summary>
/// Dynamically evaluates permissions by querying scoped roles from the database.
/// Implements the permission resolution strategy:
///   1. SuperAdmin → full access (bypass)
///   2. Project scope → check project role, fall back to org role
///   3. Organization scope → check org role
///   4. Map role → permissions via AppPermissions.GetPermissionsForRole()
/// </summary>
public class PermissionEvaluator : IPermissionEvaluator
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionEvaluator(AppDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    private async Task<bool> IsSuperAdminAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);
    }

    public async Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        ScopeType scopeType,
        Guid scopeId,
        CancellationToken cancellationToken = default)
    {
        // 1. SuperAdmin bypasses all checks
        if (await IsSuperAdminAsync(userId, cancellationToken))
            return true;

        // 2. Resolve the user's effective role in the scope
        var role = await ResolveEffectiveRoleAsync(userId, scopeType, scopeId, cancellationToken);
        if (role is null)
            return false;

        // 3. Map role → permissions and check
        var permissions = AppPermissions.GetPermissionsForRole(role);
        return permissions.Contains(permission);
    }

    public async Task<string?> GetUserRoleInScopeAsync(
        string userId,
        ScopeType scopeType,
        Guid scopeId,
        CancellationToken cancellationToken = default)
    {
        return scopeType switch
        {
            ScopeType.Organization => await GetOrgRoleAsync(userId, scopeId, cancellationToken),
            ScopeType.Project => await GetProjectRoleAsync(userId, scopeId, cancellationToken),
            _ => null
        };
    }

    public async Task<UserPermissionsDto> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var isSuperAdmin = await IsSuperAdminAsync(userId, cancellationToken);

        // Get all org memberships
        var orgMemberships = await _dbContext.UserOrganizations
            .AsNoTracking()
            .Include(uo => uo.Organization)
            .Where(uo => uo.UserId == userId)
            .ToListAsync(cancellationToken);

        var orgRoles = orgMemberships.Select(uo => new OrganizationRoleDto
        {
            OrganizationId = uo.OrganizationId,
            OrganizationName = uo.Organization.Name,
            Role = uo.Role,
            Permissions = isSuperAdmin
                ? AppPermissions.GetAllPermissions()
                : AppPermissions.GetPermissionsForRole(uo.Role)
        }).ToList();

        // Get all project memberships
        var projectMemberships = await _dbContext.UserProjects
            .AsNoTracking()
            .Include(up => up.Project)
            .Where(up => up.UserId == userId)
            .ToListAsync(cancellationToken);

        var projectRoles = projectMemberships.Select(up => new ProjectRoleDto
        {
            ProjectId = up.ProjectId,
            ProjectName = up.Project.Name,
            OrganizationId = up.Project.OrganizationId,
            Role = up.Role,
            Permissions = isSuperAdmin
                ? AppPermissions.GetAllPermissions()
                : AppPermissions.GetPermissionsForRole(up.Role)
        }).ToList();

        return new UserPermissionsDto
        {
            IsSuperAdmin = isSuperAdmin,
            OrganizationRoles = orgRoles,
            ProjectRoles = projectRoles
        };
    }

    /// <summary>
    /// Resolves the effective role for a user in a given scope.
    /// For project scope: project role takes priority, falls back to org role.
    /// </summary>
    private async Task<string?> ResolveEffectiveRoleAsync(
        string userId, ScopeType scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        if (scopeType == ScopeType.Organization)
        {
            return await GetOrgRoleAsync(userId, scopeId, cancellationToken);
        }

        // Project scope: check direct project membership first
        var projectRole = await GetProjectRoleAsync(userId, scopeId, cancellationToken);
        if (projectRole is not null)
            return projectRole;

        // Fall back to org role (OrgAdmin has implicit project access)
        var orgId = await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == scopeId)
            .Select(p => p.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (orgId == Guid.Empty)
            return null;

        return await GetOrgRoleAsync(userId, orgId, cancellationToken);
    }

    private async Task<string?> GetOrgRoleAsync(string userId, Guid orgId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserOrganizations
            .AsNoTracking()
            .Where(uo => uo.UserId == userId && uo.OrganizationId == orgId)
            .Select(uo => uo.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string?> GetProjectRoleAsync(string userId, Guid projectId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserProjects
            .AsNoTracking()
            .Where(up => up.UserId == userId && up.ProjectId == projectId)
            .Select(up => up.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
