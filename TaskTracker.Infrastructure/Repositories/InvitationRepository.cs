using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly AppDbContext _dbContext;

    public InvitationRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<Invitation?> GetActiveByScopeAndEmailAsync(
        ScopeType scopeType, Guid scopeId, string email, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations
            .FirstOrDefaultAsync(i =>
                i.ScopeType == scopeType &&
                i.ScopeId == scopeId &&
                i.InviteeEmail == email.ToLowerInvariant() &&
                i.Status == InvitationStatus.Pending,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Invitation>> GetByScopeAsync(
        ScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invitations
            .Include(i => i.InvitedByUser)
            .Where(i => i.ScopeType == scopeType && i.ScopeId == scopeId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invitations.AddAsync(invitation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        _dbContext.Invitations.Update(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
