using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Interfaces;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// Returns the active (Pending) invitation for a specific scope + email, if any.
    Task<Invitation?> GetActiveByScopeAndEmailAsync(
        ScopeType scopeType, Guid scopeId, string email, CancellationToken cancellationToken = default);

    /// Returns all invitations for a scope (all statuses).
    Task<IReadOnlyList<Invitation>> GetByScopeAsync(
        ScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default);

    Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default);
}
