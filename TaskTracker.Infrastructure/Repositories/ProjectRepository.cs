using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;

    public ProjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Project> Query() => _context.Projects.AsNoTracking();

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Projects.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Project?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Projects.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Project project, CancellationToken cancellationToken = default)
    {
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync(cancellationToken);
    }
}