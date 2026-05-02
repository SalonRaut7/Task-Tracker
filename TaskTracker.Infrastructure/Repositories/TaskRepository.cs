using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public Task<TaskItem?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Tasks
            .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
    }

    public Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == projectId, cancellationToken);
    }

    public Task<bool> EpicBelongsToProjectAsync(Guid epicId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return _context.Epics
            .AsNoTracking()
            .AnyAsync(epic => epic.Id == epicId && epic.ProjectId == projectId, cancellationToken);
    }

    public Task<bool> SprintBelongsToProjectAsync(Guid sprintId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return _context.Sprints
            .AsNoTracking()
            .AnyAsync(sprint => sprint.Id == sprintId && sprint.ProjectId == projectId, cancellationToken);
    }

    public Task<bool> CanAssignUserToProjectAsync(string userId, Guid projectId, CancellationToken cancellationToken = default)
    {
        return _context.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .AnyAsync(
                project => _context.UserProjects
                    .Any(membership => membership.UserId == userId && membership.ProjectId == project.Id)
                    && _context.UserOrganizations
                        .Any(membership => membership.UserId == userId && membership.OrganizationId == project.OrganizationId),
                cancellationToken);
    }

    public IQueryable<TaskItem> Query()
    {
        return _context.Tasks.AsNoTracking();
    }

    public Task<int> CountAsync(IQueryable<TaskItem> query, CancellationToken cancellationToken = default)
    {
        return query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.Tasks.AddAsync(task, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
