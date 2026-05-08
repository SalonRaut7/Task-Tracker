using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class TaskAttachmentRepository : ITaskAttachmentRepository
{
    private readonly AppDbContext _context;

    public TaskAttachmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.TaskAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
        => await _context.TaskAttachments
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
        => _context.TaskAttachments
            .AsNoTracking()
            .CountAsync(a => a.TaskId == taskId, cancellationToken);

    public Task<bool> TaskExistsAsync(int taskId, CancellationToken cancellationToken = default)
        => _context.Tasks
            .AsNoTracking()
            .AnyAsync(t => t.Id == taskId, cancellationToken);

    public async Task AddAsync(TaskAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _context.TaskAttachments.AddAsync(attachment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskAttachment attachment, CancellationToken cancellationToken = default)
    {
        _context.TaskAttachments.Remove(attachment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetProjectIdByTaskIdAsync(int taskId, CancellationToken cancellationToken = default)
        => await _context.Tasks
            .AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => (Guid?)t.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);
}
