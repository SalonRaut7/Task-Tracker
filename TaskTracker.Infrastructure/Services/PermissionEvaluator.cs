using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Constants;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Services;
// Dynamically evaluates permissions by querying scoped roles from the database.
// All hot-path lookups are wrapped in ICacheService to eliminate repeated DB hits
// for the same user within a request window.

// Invalidation strategy:
//    - SuperAdmin check    → invalidated by role-change operations (rare)
//   - Org/project role    → invalidated by MembershipRepository on every mutation
//   - Full permissions    → invalidated by MembershipRepository on every mutation
public class PermissionEvaluator : IPermissionEvaluator
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICacheService _cache;
    private readonly CacheOptions _cacheOptions;

    public PermissionEvaluator(
        AppDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICacheService cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    private Task<bool> IsSuperAdminAsync(string userId, CancellationToken cancellationToken = default)
        => _cache.GetOrCreateAsync(
            CacheKeys.UserIsSuperAdmin(userId),
            async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null) return false;
                return await _userManager.IsInRoleAsync(user, AppRoles.SuperAdmin);
            },
            slidingExpiration: _cacheOptions.SuperAdminSliding,
            absoluteExpiration: _cacheOptions.SuperAdminAbsolute,
            cancellationToken: cancellationToken);

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

        // 2. Resolve the user's effective role in the scope (cached per role lookup)
        var role = await ResolveEffectiveRoleAsync(userId, scopeType, scopeId, cancellationToken);
        if (role is null)
            return false;

        // 3. Map role → permissions and check (pure in-memory, no DB)
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
        // Cache the entire permissions bundle — this is what /api/me/permissions returns.
        // It is invalidated on any membership mutation via MembershipRepository.
        return await _cache.GetOrCreateAsync(
            CacheKeys.UserPermissions(userId),
            async () => await LoadPermissionsFromDbAsync(userId, cancellationToken),
            slidingExpiration: _cacheOptions.PermissionsSliding,
            absoluteExpiration: _cacheOptions.PermissionsAbsolute,
            cancellationToken: cancellationToken);
    }

    // Private helpers

    private async Task<UserPermissionsDto> LoadPermissionsFromDbAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var isSuperAdmin = await IsSuperAdminAsync(userId, cancellationToken);

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

    // Resolves the effective role for a user in a given scope.
    // For project scope: project role takes priority, falls back to org role.
    // Both lookups are cached individually.
    private async Task<string?> ResolveEffectiveRoleAsync(
        string userId, ScopeType scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        if (scopeType == ScopeType.Organization)
            return await GetOrgRoleAsync(userId, scopeId, cancellationToken);

        // Project scope: check direct project membership first (cached)
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

    private Task<string?> GetOrgRoleAsync(string userId, Guid orgId, CancellationToken cancellationToken)
        => _cache.GetOrCreateAsync<string?>(
            CacheKeys.UserOrgRole(userId, orgId),
            () => _dbContext.UserOrganizations
                .AsNoTracking()
                .Where(uo => uo.UserId == userId && uo.OrganizationId == orgId)
                .Select(uo => uo.Role)
                .FirstOrDefaultAsync(cancellationToken),
            slidingExpiration: _cacheOptions.RoleInScopeSliding,
            absoluteExpiration: _cacheOptions.RoleInScopeAbsolute,
            cancellationToken: cancellationToken);

    private Task<string?> GetProjectRoleAsync(string userId, Guid projectId, CancellationToken cancellationToken)
        => _cache.GetOrCreateAsync<string?>(
            CacheKeys.UserProjectRole(userId, projectId),
            () => _dbContext.UserProjects
                .AsNoTracking()
                .Where(up => up.UserId == userId && up.ProjectId == projectId)
                .Select(up => up.Role)
                .FirstOrDefaultAsync(cancellationToken),
            slidingExpiration: _cacheOptions.RoleInScopeSliding,
            absoluteExpiration: _cacheOptions.RoleInScopeAbsolute,
            cancellationToken: cancellationToken);
}
