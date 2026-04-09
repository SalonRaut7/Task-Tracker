using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _context;

        public TaskRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Tasks
                .AsNoTracking()
                .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        }

        public async Task<TaskItem?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Tasks
                .FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
        }

        public IQueryable<TaskItem> Query()
        {
            return _context.Tasks.AsNoTracking();
        }

        public Task<int> CountAsync(IQueryable<TaskItem> query, CancellationToken cancellationToken = default)
        {
            return query.CountAsync(cancellationToken);
        }

        public Task<List<TaskItem>> ToListAsync(IQueryable<TaskItem> query, CancellationToken cancellationToken = default)
        {
            return query.ToListAsync(cancellationToken);
        }

        public async Task<List<TaskItem>> ListAsync(
            string? titleContains = null,
            Status? status = null,
            TaskPriority? priority = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<TaskItem> query = Query();

            if (!string.IsNullOrWhiteSpace(titleContains))
            {
                query = query.Where(task =>
                    EF.Functions.ILike(task.Title, $"%{titleContains}%"));
            }

            if (status.HasValue)
            {
                query = query.Where(task => task.Status == status.Value);
            }
            if (priority.HasValue)
            {
                query = query.Where(task => task.Priority == priority.Value);
            }

            return await query
                .OrderByDescending(task => task.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            await _context.Tasks.AddAsync(task, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}