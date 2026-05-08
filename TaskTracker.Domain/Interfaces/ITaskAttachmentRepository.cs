using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;

public interface ITaskAttachmentRepository
{
    Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<int> CountByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
    Task<bool> TaskExistsAsync(int taskId, CancellationToken cancellationToken = default);
    Task AddAsync(TaskAttachment attachment, CancellationToken cancellationToken = default);
    Task DeleteAsync(TaskAttachment attachment, CancellationToken cancellationToken = default);
    Task<Guid?> GetProjectIdByTaskIdAsync(int taskId, CancellationToken cancellationToken = default);
}
