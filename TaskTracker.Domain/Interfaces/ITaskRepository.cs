using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Interfaces
{
    public interface ITaskRepository
    {
        Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<TaskItem?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> ProjectExistsAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task<bool> EpicBelongsToProjectAsync(Guid epicId, Guid projectId, CancellationToken cancellationToken = default);
        Task<bool> SprintBelongsToProjectAsync(Guid sprintId, Guid projectId, CancellationToken cancellationToken = default);
        Task<bool> CanAssignUserToProjectAsync(string userId, Guid projectId, CancellationToken cancellationToken = default);
        IQueryable<TaskItem> Query();
        Task<int> CountAsync(IQueryable<TaskItem> query, CancellationToken cancellationToken = default);
        Task<List<TaskItem>> ToListAsync(IQueryable<TaskItem> query, CancellationToken cancellationToken = default);
        Task<List<TaskItem>> ListAsync(string? titleContains = null, Status? status = null, TaskPriority? priority = null, CancellationToken cancellationToken = default);
        Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default);
    }
}