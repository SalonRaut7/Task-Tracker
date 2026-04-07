using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Interfaces
{
    public interface ITaskRepository
    {
        Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<TaskItem?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
        Task<List<TaskItem>> ListAsync(string? titleContains = null, Status? status = null, TaskPriority? priority = null, CancellationToken cancellationToken = default);
        Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default);
        Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default);
    }
}