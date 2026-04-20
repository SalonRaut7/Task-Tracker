using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;

public interface IEpicRepository
{
    IQueryable<Epic> Query();
    Task<Epic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Epic?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Epic epic, CancellationToken cancellationToken = default);
    Task UpdateAsync(Epic epic, CancellationToken cancellationToken = default);
    Task DeleteAsync(Epic epic, CancellationToken cancellationToken = default);
}
