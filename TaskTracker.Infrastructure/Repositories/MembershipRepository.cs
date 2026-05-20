using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Constants;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class MembershipRepository : IMembershipRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly CacheOptions _cacheOptions;

    public MembershipRepository(
        AppDbContext dbContext,
        ICacheService cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _dbContext = dbContext;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    // Queries (cache-backed)
    public Task<IReadOnlyList<Guid>> GetUserOrganizationIdsAsync(string userId, CancellationToken ct = default)
        => _cache.GetOrCreateAsync<IReadOnlyList<Guid>>(
            CacheKeys.UserOrgIds(userId),
            async () =>
            {
                var result = await _dbContext.UserOrganizations
                    .AsNoTracking()
                    .Where(uo => uo.UserId == userId)
                    .Select(uo => uo.OrganizationId)
                    .ToListAsync(ct);
                return (IReadOnlyList<Guid>)result;
            },
            slidingExpiration: _cacheOptions.MembershipIdsSliding,
            absoluteExpiration: _cacheOptions.MembershipIdsAbsolute,
            cancellationToken: ct);

    public Task<IReadOnlyList<Guid>> GetUserProjectIdsAsync(string userId, CancellationToken ct = default)
        => _cache.GetOrCreateAsync<IReadOnlyList<Guid>>(
            CacheKeys.UserProjectIds(userId),
            async () =>
            {
                var result = await _dbContext.UserProjects
                    .AsNoTracking()
                    .Where(up => up.UserId == userId)
                    .Select(up => up.ProjectId)
                    .ToListAsync(ct);
                return (IReadOnlyList<Guid>)result;
            },
            slidingExpiration: _cacheOptions.MembershipIdsSliding,
            absoluteExpiration: _cacheOptions.MembershipIdsAbsolute,
            cancellationToken: ct);

    public async Task<bool> IsOrganizationMemberAsync(string userId, Guid organizationId, CancellationToken ct = default)
    {
        return await _dbContext.UserOrganizations
            .AsNoTracking()
            .AnyAsync(uo => uo.UserId == userId && uo.OrganizationId == organizationId, ct);
    }

    public async Task<List<UserOrganization>> GetOrganizationMembershipsAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await _dbContext.UserOrganizations
            .AsNoTracking()
            .Include(uo => uo.User)
            .Where(uo => uo.OrganizationId == organizationId)
            .ToListAsync(ct);
    }

    public async Task<List<UserProject>> GetProjectMembershipsAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _dbContext.UserProjects
            .AsNoTracking()
            .Include(up => up.User)
            .Where(up => up.ProjectId == projectId)
            .ToListAsync(ct);
    }

    // Mutations (DB write + cache invalidation)

    public async Task UpsertOrganizationMemberAsync(
        string userId, Guid organizationId, string role, string? invitedByUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var existing = await _dbContext.UserOrganizations
            .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OrganizationId == organizationId, ct);

        if (existing is not null)
        {
            existing.Role = role;
            existing.UpdatedAt = now;
        }
        else
        {
            _dbContext.UserOrganizations.Add(new UserOrganization
            {
                UserId = userId,
                OrganizationId = organizationId,
                Role = role,
                InvitedByUserId = invitedByUserId,
                JoinedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(ct);
        InvalidateUserMembershipCache(userId, organizationId: organizationId);
    }

    public async Task UpsertProjectMemberAsync(
        string userId, Guid projectId, string role, string? invitedByUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new InvalidOperationException("Project not found.");

        var hasOrgMembership = await _dbContext.UserOrganizations
            .AnyAsync(uo => uo.UserId == userId && uo.OrganizationId == project.OrganizationId, ct);

        if (!hasOrgMembership)
            throw new InvalidOperationException(
                "You must be a member of the organization before joining a project.");

        var existing = await _dbContext.UserProjects
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId, ct);

        if (existing is not null)
        {
            existing.Role = role;
            existing.UpdatedAt = now;
        }
        else
        {
            _dbContext.UserProjects.Add(new UserProject
            {
                UserId = userId,
                ProjectId = projectId,
                Role = role,
                InvitedByUserId = invitedByUserId,
                JoinedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(ct);
        InvalidateUserMembershipCache(userId, projectId: projectId);
    }

    public async Task<UserOrganization> UpdateOrganizationMemberRoleAsync(
        string userId, Guid organizationId, string newRole, CancellationToken ct = default)
    {
        var membership = await _dbContext.UserOrganizations
            .Include(uo => uo.User)
            .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OrganizationId == organizationId, ct)
            ?? throw new InvalidOperationException("Member not found in this organization.");

        membership.Role = newRole;
        membership.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        InvalidateUserMembershipCache(userId, organizationId: organizationId);
        return membership;
    }

    public async Task<UserProject> UpdateProjectMemberRoleAsync(
        string userId, Guid projectId, string newRole, CancellationToken ct = default)
    {
        var membership = await _dbContext.UserProjects
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId, ct)
            ?? throw new InvalidOperationException("Member not found in this project.");

        membership.Role = newRole;
        membership.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        InvalidateUserMembershipCache(userId, projectId: projectId);
        return membership;
    }

    public async Task RemoveOrganizationMemberAsync(string userId, Guid organizationId, CancellationToken ct = default)
    {
        var membership = await _dbContext.UserOrganizations
            .FirstOrDefaultAsync(uo => uo.UserId == userId && uo.OrganizationId == organizationId, ct)
            ?? throw new InvalidOperationException("Member not found.");

        // Cascade: remove all project memberships in this org
        var orgProjectIds = await _dbContext.Projects
            .AsNoTracking()
            .Where(p => p.OrganizationId == organizationId)
            .Select(p => p.Id)
            .ToListAsync(ct);

        var projectMemberships = await _dbContext.UserProjects
            .Where(up => up.UserId == userId && orgProjectIds.Contains(up.ProjectId))
            .ToListAsync(ct);

        _dbContext.UserProjects.RemoveRange(projectMemberships);
        _dbContext.UserOrganizations.Remove(membership);
        await _dbContext.SaveChangesAsync(ct);

        // Invalidate all user membership cache (org + all project roles lost)
        InvalidateAllUserCache(userId);
    }

    public async Task RemoveProjectMemberAsync(string userId, Guid projectId, CancellationToken ct = default)
    {
        var membership = await _dbContext.UserProjects
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId, ct)
            ?? throw new InvalidOperationException("Member not found.");

        _dbContext.UserProjects.Remove(membership);
        await _dbContext.SaveChangesAsync(ct);

        InvalidateUserMembershipCache(userId, projectId: projectId);
    }

    // Cache invalidation helpers

    // Invalidates membership ID lists, role-in-scope, and the permissions bundle
    // for the specified user after an org or project membership mutation.
    private void InvalidateUserMembershipCache(string userId, Guid? organizationId = null, Guid? projectId = null)
    {
        // Always invalidate the permissions bundle and both ID lists
        _cache.Remove(CacheKeys.UserPermissions(userId));
        _cache.Remove(CacheKeys.UserOrgIds(userId));
        _cache.Remove(CacheKeys.UserProjectIds(userId));

        // Invalidate the specific role-in-scope entry
        if (organizationId.HasValue)
            _cache.Remove(CacheKeys.UserOrgRole(userId, organizationId.Value));

        if (projectId.HasValue)
            _cache.Remove(CacheKeys.UserProjectRole(userId, projectId.Value));
    }

    // Invalidates all user-scoped cache entries (used when removing from org,
    // which cascades across all project memberships too).
    private void InvalidateAllUserCache(string userId)
    {
        _cache.Remove(CacheKeys.UserPermissions(userId));
        _cache.Remove(CacheKeys.UserOrgIds(userId));
        _cache.Remove(CacheKeys.UserProjectIds(userId));
        _cache.RemoveByPrefix($"cache:user-org-role:{userId}:");
        _cache.RemoveByPrefix($"cache:user-proj-role:{userId}:");
    }
}
