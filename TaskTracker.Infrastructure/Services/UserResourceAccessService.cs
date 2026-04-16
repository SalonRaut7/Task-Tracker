using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Services;

public class UserResourceAccessService : IUserResourceAccessService
{
    private readonly AppDbContext _dbContext;

    public UserResourceAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanAccessOrganizationAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await UserOrganizationIds(userId)
            .AnyAsync(id => id == organizationId, cancellationToken);
    }

    public async Task<bool> CanAccessProjectAsync(
        string userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var userOrganizationIds = UserOrganizationIds(userId);

        return await _dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .AnyAsync(project => userOrganizationIds.Contains(project.OrganizationId), cancellationToken);
    }

    public async Task<bool> CanAccessCommentAsync(
        string userId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var userOrganizationIds = UserOrganizationIds(userId);

        return await _dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.Id == commentId)
            .Join(
                _dbContext.Users.AsNoTracking(),
                comment => comment.AuthorId,
                author => author.Id,
                (comment, author) => new { comment.AuthorId, author.OrganizationId })
            .AnyAsync(
                row => row.AuthorId == userId
                    || (row.OrganizationId.HasValue && userOrganizationIds.Contains(row.OrganizationId.Value)),
                cancellationToken);
    }

    public async Task<bool> CanAccessEpicAsync(
        string userId,
        Guid epicId,
        CancellationToken cancellationToken = default)
    {
        var userOrganizationIds = UserOrganizationIds(userId);

        return await _dbContext.Epics
            .AsNoTracking()
            .Where(epic => epic.Id == epicId)
            .Select(epic => epic.Project.OrganizationId)
            .AnyAsync(orgId => userOrganizationIds.Contains(orgId), cancellationToken);
    }

    public async Task<bool> CanAccessSprintAsync(
        string userId,
        Guid sprintId,
        CancellationToken cancellationToken = default)
    {
        var userOrganizationIds = UserOrganizationIds(userId);

        return await _dbContext.Sprints
            .AsNoTracking()
            .Where(sprint => sprint.Id == sprintId)
            .Select(sprint => sprint.Project.OrganizationId)
            .AnyAsync(orgId => userOrganizationIds.Contains(orgId), cancellationToken);
    }

    private IQueryable<Guid> UserOrganizationIds(string userId)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.OrganizationId.HasValue)
            .Select(user => user.OrganizationId!.Value);
    }
}