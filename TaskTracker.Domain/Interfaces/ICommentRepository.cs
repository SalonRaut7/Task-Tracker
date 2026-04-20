using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Interfaces;

public interface ICommentRepository
{
    IQueryable<Comment> Query();
    Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Comment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> TaskExistsAsync(int taskId, CancellationToken cancellationToken = default);
    Task<(string FirstName, string LastName)?> GetAuthorNameAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Comment comment, CancellationToken cancellationToken = default);
}
