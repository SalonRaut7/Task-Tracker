using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetFullNameAsync(string userId, CancellationToken cancellationToken = default)
    {
        var names = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.FirstName, u.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        return names is null ? null : $"{names.FirstName} {names.LastName}".Trim();
    }

    public async Task<IReadOnlyList<string>> GetSuperAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Join(
                _dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .Where(item => item.Name == AppRoles.SuperAdmin)
            .Join(
                _dbContext.Users.AsNoTracking(),
                roleItem => roleItem.UserId,
                user => user.Id,
                (roleItem, user) => new { roleItem.UserId, user.IsActive, user.IsArchived })
            .Where(item => item.IsActive && !item.IsArchived)
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSummaryReadModel>> GetUserSummariesAsync(
        bool archived,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsArchived == archived)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .ThenBy(user => user.Email)
            .Select(user => new UserSummaryReadModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsSuperAdmin = _dbContext.UserRoles
                    .Join(
                        _dbContext.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => new { userRole.UserId, role.Name })
                    .Any(item => item.UserId == user.Id && item.Name == AppRoles.SuperAdmin),
                IsActive = user.IsActive,
                IsArchived = user.IsArchived,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                ArchivedAtUtc = user.ArchivedAtUtc,
                ArchivedByUserId = user.ArchivedByUserId,
                ArchiveReason = user.ArchiveReason,
                OrganizationCount = _dbContext.UserOrganizations.Count(m => m.UserId == user.Id),
                ProjectCount = _dbContext.UserProjects.Count(m => m.UserId == user.Id),
                AssignedTaskCount = _dbContext.Tasks.Count(task => task.AssigneeId == user.Id),
                ReportedTaskCount = _dbContext.Tasks.Count(task => task.ReporterId == user.Id),
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDetailsReadModel?> GetUserDetailsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(item => item.Id == userId)
            .Select(item => new UserDetailsReadModel
            {
                UserId = item.Id,
                Email = item.Email ?? string.Empty,
                FirstName = item.FirstName,
                LastName = item.LastName,
                IsActive = item.IsActive,
                IsArchived = item.IsArchived,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                ArchivedAtUtc = item.ArchivedAtUtc,
                ArchivedByUserId = item.ArchivedByUserId,
                ArchiveReason = item.ArchiveReason,
                AssignedTaskCount = _dbContext.Tasks.Count(task => task.AssigneeId == item.Id),
                ReportedTaskCount = _dbContext.Tasks.Count(task => task.ReporterId == item.Id),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var organizationMemberships = await _dbContext.UserOrganizations
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .OrderBy(membership => membership.Organization.Name)
            .Select(membership => new UserOrganizationMembershipReadModel
            {
                OrganizationId = membership.OrganizationId,
                OrganizationName = membership.Organization.Name,
                Role = membership.Role,
                JoinedAt = membership.JoinedAt,
            })
            .ToListAsync(cancellationToken);

        var projectMemberships = await _dbContext.UserProjects
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .OrderBy(membership => membership.Project.Name)
            .Select(membership => new UserProjectMembershipReadModel
            {
                ProjectId = membership.ProjectId,
                ProjectName = membership.Project.Name,
                OrganizationId = membership.Project.OrganizationId,
                OrganizationName = membership.Project.Organization.Name,
                Role = membership.Role,
                JoinedAt = membership.JoinedAt,
            })
            .ToListAsync(cancellationToken);

        return new UserDetailsReadModel
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            IsArchived = user.IsArchived,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            ArchivedAtUtc = user.ArchivedAtUtc,
            ArchivedByUserId = user.ArchivedByUserId,
            ArchiveReason = user.ArchiveReason,
            AssignedTaskCount = user.AssignedTaskCount,
            ReportedTaskCount = user.ReportedTaskCount,
            OrganizationMemberships = organizationMemberships,
            ProjectMemberships = projectMemberships,
        };
    }

    public Task<ApplicationUser?> GetByIdForUpdateAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task ArchiveAsync(
        ApplicationUser user,
        string archivedByUserId,
        string? archiveReason,
        CancellationToken cancellationToken = default)
    {
        user.IsArchived = true;
        user.IsActive = false;
        user.ArchivedAtUtc = DateTime.UtcNow;
        user.ArchivedByUserId = archivedByUserId;
        user.ArchiveReason = string.IsNullOrWhiteSpace(archiveReason)
            ? null
            : archiveReason.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(
        ApplicationUser user,
        CancellationToken cancellationToken = default)
    {
        user.IsArchived = false;
        user.IsActive = true;
        user.ArchivedAtUtc = null;
        user.ArchivedByUserId = null;
        user.ArchiveReason = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReassignReportedTasksAsync(
        string sourceUserId,
        string targetUserId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        await _dbContext.Tasks
            .Where(task => task.ReporterId == sourceUserId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(task => task.ReporterId, targetUserId)
                    .SetProperty(task => task.UpdatedAt, nowUtc),
                cancellationToken);
    }

    public async Task ClearTaskAssignmentsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        await _dbContext.Tasks
            .Where(task => task.AssigneeId == userId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(task => task.AssigneeId, (string?)null)
                    .SetProperty(task => task.UpdatedAt, nowUtc),
                cancellationToken);
    }
}
