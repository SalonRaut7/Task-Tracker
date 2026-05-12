using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class SprintRepository : ISprintRepository
{
    private readonly AppDbContext _context;

    public SprintRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Sprint> Query() => _context.Sprints.AsNoTracking();

    public Task<Sprint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Sprints.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Sprint?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Sprints.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Sprint?> GetByIdWithTasksAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Sprints
            .Include(s => s.Tasks)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> HasActiveSprintAsync(Guid projectId, Guid? excludeSprintId = null, CancellationToken cancellationToken = default)
        => _context.Sprints
            .Where(s => s.ProjectId == projectId)
            .Where(s => s.Status == SprintStatus.Active)
            .Where(s => excludeSprintId == null || s.Id != excludeSprintId)
            .AnyAsync(cancellationToken);

    public Task<bool> HasOverlappingSprintAsync(Guid projectId, DateOnly startDate, DateOnly endDate, Guid? excludeSprintId = null, CancellationToken cancellationToken = default)
        => _context.Sprints
            .Where(s => s.ProjectId == projectId)
            .Where(s => s.Status != SprintStatus.Cancelled && s.Status != SprintStatus.Archived)
            .Where(s => excludeSprintId == null || s.Id != excludeSprintId)
            .Where(s => s.StartDate <= endDate && s.EndDate >= startDate)
            .AnyAsync(cancellationToken);

    public async Task AddAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        await _context.Sprints.AddAsync(sprint, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        _context.Sprints.Update(sprint);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Sprint sprint, CancellationToken cancellationToken = default)
    {
        _context.Sprints.Remove(sprint);
        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task ArchiveAsync(Sprint sprint, string archivedByUserId, string archiveReason, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var today  = DateOnly.FromDateTime(utcNow);

        sprint.TransitionTo(
            SprintStatus.Archived,
            today,
            utcNow,
            archiveReason,
            archivedByUserId);

        _context.Sprints.Update(sprint);
        await _context.SaveChangesAsync(cancellationToken);
    }
}