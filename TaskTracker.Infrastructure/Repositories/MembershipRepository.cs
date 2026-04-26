using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class MembershipRepository : IMembershipRepository
{
    private readonly AppDbContext _dbContext;

    public MembershipRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // ── Queries ──────────────────────────────────────────────

    public async Task<IReadOnlyList<Guid>> GetUserOrganizationIdsAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.UserOrganizations
            .AsNoTracking()
            .Where(uo => uo.UserId == userId)
            .Select(uo => uo.OrganizationId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetUserProjectIdsAsync(string userId, CancellationToken ct = default)
    {
        return await _dbContext.UserProjects
            .AsNoTracking()
            .Where(up => up.UserId == userId)
            .Select(up => up.ProjectId)
            .ToListAsync(ct);
    }

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

    // ── Mutations ─────────────────────────────────────────────

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
    }

    public async Task UpsertProjectMemberAsync(
        string userId, Guid projectId, string role, string? invitedByUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Defensive: verify org membership
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
    }

    public async Task RemoveProjectMemberAsync(string userId, Guid projectId, CancellationToken ct = default)
    {
        var membership = await _dbContext.UserProjects
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProjectId == projectId, ct)
            ?? throw new InvalidOperationException("Member not found.");

        _dbContext.UserProjects.Remove(membership);
        await _dbContext.SaveChangesAsync(ct);
    }
}
