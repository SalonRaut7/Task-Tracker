using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;

public interface ISprintRepository
{
    IQueryable<Sprint> Query();

    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Sprint?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Loads the sprint including its Tasks collection — required for lifecycle commands.
    Task<Sprint?> GetByIdWithTasksAsync(Guid id, CancellationToken cancellationToken = default);
    // Returns true if the project already has a sprint in Active status
    // (optionally excluding a specific sprint id for update scenarios).
    Task<bool> HasActiveSprintAsync(Guid projectId, Guid? excludeSprintId = null, CancellationToken cancellationToken = default);
    // Returns true if any non-cancelled/archived sprint in the project overlaps
    // the given date range (optionally excluding a specific sprint id).
    Task ArchiveAsync(Sprint sprint, string archivedByUserId, string archiveReason, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingSprintAsync(Guid projectId, DateOnly startDate, DateOnly endDate, Guid? excludeSprintId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default);
    Task DeleteAsync(Sprint sprint, CancellationToken cancellationToken = default);
}
