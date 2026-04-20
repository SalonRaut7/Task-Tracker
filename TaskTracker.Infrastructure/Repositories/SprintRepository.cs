using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
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
        => _context.Sprints.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Sprint?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Sprints.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

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
}