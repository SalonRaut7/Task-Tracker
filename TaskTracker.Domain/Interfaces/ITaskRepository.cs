using TaskTracker.Domain.Entities;

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
        Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task<string?> GetProjectKeyAsync(Guid projectId, CancellationToken cancellationToken = default);
        Task UpdateTaskCodesForProjectAsync(Guid projectId, string newProjectKey, CancellationToken cancellationToken = default);

        // Returns tracked TaskItem entities (for update) keyed by TaskCode, filtered to the given project. Used exclusively by the import handler.
        Task<Dictionary<string, TaskItem>> GetByTaskCodesForUpdateAsync(
            Guid projectId,
            IEnumerable<string> taskCodes,
            CancellationToken cancellationToken = default);
    }
}
