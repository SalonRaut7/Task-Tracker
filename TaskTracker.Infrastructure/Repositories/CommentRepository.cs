using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _context;

    public CommentRepository(AppDbContext context)
    {
        _context = context;
    }

    public IQueryable<Comment> Query() => _context.Comments.AsNoTracking();

    public Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Comments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<Comment?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Comments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<bool> TaskExistsAsync(int taskId, CancellationToken cancellationToken = default)
        => _context.Tasks.AsNoTracking().AnyAsync(task => task.Id == taskId, cancellationToken);

    public async Task<(string FirstName, string LastName)?> GetAuthorNameAsync(string userId, CancellationToken cancellationToken = default)
    {
        var author = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.FirstName, user.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        if (author is null)
        {
            return null;
        }

        return (author.FirstName, author.LastName);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _context.Comments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _context.Comments.Update(comment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}