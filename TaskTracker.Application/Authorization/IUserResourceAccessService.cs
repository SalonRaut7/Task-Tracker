namespace TaskTracker.Application.Authorization;

public interface IUserResourceAccessService
{
    Task<bool> CanAccessOrganizationAsync(string userId, Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessProjectAsync(string userId, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessCommentAsync(string userId, Guid commentId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessEpicAsync(string userId, Guid epicId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessSprintAsync(string userId, Guid sprintId, CancellationToken cancellationToken = default);
}