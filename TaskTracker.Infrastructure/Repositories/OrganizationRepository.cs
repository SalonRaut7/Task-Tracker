using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _context;

    public OrganizationRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Organization> Query() => _context.Organizations.AsNoTracking();

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Organizations.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Organization?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Organizations.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        await _context.Organizations.AddAsync(organization, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _context.Organizations.Update(organization);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync(cancellationToken);
    }
}