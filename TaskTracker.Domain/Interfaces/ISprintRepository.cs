using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;

public interface ISprintRepository
{
    IQueryable<Sprint> Query();
    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sprint?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task DeleteAsync(Sprint sprint, CancellationToken cancellationToken = default);
}
