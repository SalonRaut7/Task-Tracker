using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class EpicRepository : IEpicRepository
{
    private readonly AppDbContext _context;

    public EpicRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Epic> Query() => _context.Epics.AsNoTracking();

    public Task<Epic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Epics.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Epic?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Epics.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task AddAsync(Epic epic, CancellationToken cancellationToken = default)
    {
        await _context.Epics.AddAsync(epic, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Epic epic, CancellationToken cancellationToken = default)
    {
        _context.Epics.Update(epic);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Epic epic, CancellationToken cancellationToken = default)
    {
        _context.Epics.Remove(epic);
        await _context.SaveChangesAsync(cancellationToken);
    }
}