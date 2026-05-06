using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Domain.ReadModels;
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

    public async Task<IReadOnlyList<CommentMentionableUser>> GetMentionableUsersForTaskAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var projectId = await _context.Tasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => (Guid?)task.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!projectId.HasValue)
        {
            return [];
        }

        var projectMembers = await _context.UserProjects
            .AsNoTracking()
            .Where(membership =>
                membership.ProjectId == projectId.Value
                && membership.User.IsActive
                && !membership.User.IsArchived)
            .Select(membership => new CommentMentionableUser
            {
                UserId = membership.UserId,
                FirstName = membership.User.FirstName,
                LastName = membership.User.LastName,
                Role = membership.Role
            })
            .ToListAsync(cancellationToken);

        var superAdmins = await (
                from user in _context.Users.AsNoTracking()
                join userRole in _context.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                where role.Name == AppRoles.SuperAdmin
                    && user.IsActive
                    && !user.IsArchived
                select new CommentMentionableUser
                {
                    UserId = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = AppRoles.SuperAdmin
                })
            .ToListAsync(cancellationToken);

        return projectMembers
            .Concat(superAdmins)
            .GroupBy(user => user.UserId, StringComparer.Ordinal)
            .Select(group =>
                group.FirstOrDefault(user => user.Role == AppRoles.SuperAdmin)
                ?? group.First())
            .OrderBy(user => string.Concat(user.FirstName, " ", user.LastName).Trim())
            .ThenBy(user => user.Role)
            .ToList();
    }

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
