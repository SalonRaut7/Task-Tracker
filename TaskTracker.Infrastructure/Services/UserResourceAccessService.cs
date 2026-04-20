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

    public async Task<IReadOnlyList<Guid>> GetUserOrganizationIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryUserOrganizationIds(userId)
            .Select(membership => membership.OrganizationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetUserProjectIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await QueryUserProjectIds(userId)
            .Select(membership => membership.ProjectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessOrganizationAsync(
        string userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await QueryUserOrganizationIds(userId)
            .AnyAsync(membership => membership.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<bool> CanAccessProjectAsync(
        string userId,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var userProjectIds = QueryUserProjectIds(userId).Select(membership => membership.ProjectId);
        var userOrganizationIds = QueryUserOrganizationIds(userId).Select(membership => membership.OrganizationId);

        return await _dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .AnyAsync(
                project => userProjectIds.Contains(project.Id)
                    && userOrganizationIds.Contains(project.OrganizationId),
                cancellationToken);
    }

    public async Task<bool> CanAccessTaskAsync(
        string userId,
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var userProjectIds = QueryUserProjectIds(userId).Select(membership => membership.ProjectId);
        var userOrganizationIds = QueryUserOrganizationIds(userId).Select(membership => membership.OrganizationId);

        return await _dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .AnyAsync(
                task => userProjectIds.Contains(task.ProjectId)
                    && userOrganizationIds.Contains(task.Project.OrganizationId),
                cancellationToken);
    }

    public async Task<bool> CanAccessCommentAsync(
        string userId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var userProjectIds = QueryUserProjectIds(userId).Select(membership => membership.ProjectId);
        var userOrganizationIds = QueryUserOrganizationIds(userId).Select(membership => membership.OrganizationId);

        return await _dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.Id == commentId)
            .AnyAsync(
                comment => userProjectIds.Contains(comment.Task.ProjectId)
                    && userOrganizationIds.Contains(comment.Task.Project.OrganizationId),
                cancellationToken);
    }

    public async Task<bool> CanAccessEpicAsync(
        string userId,
        Guid epicId,
        CancellationToken cancellationToken = default)
    {
        var userProjectIds = QueryUserProjectIds(userId).Select(membership => membership.ProjectId);
        var userOrganizationIds = QueryUserOrganizationIds(userId).Select(membership => membership.OrganizationId);

        return await _dbContext.Epics
            .AsNoTracking()
            .Where(epic => epic.Id == epicId)
            .AnyAsync(
                epic => userProjectIds.Contains(epic.ProjectId)
                    && userOrganizationIds.Contains(epic.Project.OrganizationId),
                cancellationToken);
    }

    public async Task<bool> CanAccessSprintAsync(
        string userId,
        Guid sprintId,
        CancellationToken cancellationToken = default)
    {
        var userProjectIds = QueryUserProjectIds(userId).Select(membership => membership.ProjectId);
        var userOrganizationIds = QueryUserOrganizationIds(userId).Select(membership => membership.OrganizationId);

        return await _dbContext.Sprints
            .AsNoTracking()
            .Where(sprint => sprint.Id == sprintId)
            .AnyAsync(
                sprint => userProjectIds.Contains(sprint.ProjectId)
                    && userOrganizationIds.Contains(sprint.Project.OrganizationId),
                cancellationToken);
    }

    private IQueryable<Domain.Entities.UserOrganization> QueryUserOrganizationIds(string userId)
    {
        return _dbContext.UserOrganizations
            .AsNoTracking()
            .Where(membership => membership.UserId == userId);
    }

    private IQueryable<Domain.Entities.UserProject> QueryUserProjectIds(string userId)
    {
        return _dbContext.UserProjects
            .AsNoTracking()
            .Where(membership => membership.UserId == userId);
    }
}