using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Constants;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Services;

// Resolves resource IDs → project/org scope IDs for the AuthorizationBehavior pipeline.
// Resource-to-project mappings (task→project, epic→project, sprint→project) are
// cached because they are immutable after resource creation — a task never moves
// between projects. Comment→project and invitation→scope are also cached with a
// shorter TTL since they follow the resource lifecycle.
public sealed class AuthorizationScopeResolver : IAuthorizationScopeResolver
{
    private readonly AppDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly CacheOptions _cacheOptions;

    public AuthorizationScopeResolver(
        AppDbContext dbContext,
        ICacheService cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _dbContext = dbContext;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<(ScopeType scopeType, Guid scopeId)> ResolveScopeAsync(
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default)
    {
        return resourceScope.ResourceType switch
        {
            ResourceType.Organization => ResolveGuidScope(resourceScope.Id, ScopeType.Organization),
            ResourceType.Project => ResolveGuidScope(resourceScope.Id, ScopeType.Project),
            ResourceType.Task => await ResolveTaskScopeAsync(resourceScope.Id, cancellationToken),
            ResourceType.Epic => await ResolveEpicScopeAsync(resourceScope.Id, cancellationToken),
            ResourceType.Sprint => await ResolveSprintScopeAsync(resourceScope.Id, cancellationToken),
            ResourceType.Comment => await ResolveCommentScopeAsync(resourceScope.Id, cancellationToken),
            ResourceType.Invitation => await ResolveInvitationScopeAsync(resourceScope.Id, cancellationToken),
            _ => (ScopeType.Project, Guid.Empty)
        };
    }

    // Task → Project (immutable after creation — long TTL)

    private async Task<(ScopeType, Guid)> ResolveTaskScopeAsync(
        string taskIdValue, CancellationToken cancellationToken)
    {
        if (!int.TryParse(taskIdValue, out var taskId))
            return (ScopeType.Project, Guid.Empty);

        var projectId = await _cache.GetOrCreateAsync(
            CacheKeys.TaskProject(taskId),
            () => _dbContext.Tasks
                .AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(t => t.ProjectId)
                .FirstOrDefaultAsync(cancellationToken),
            slidingExpiration: _cacheOptions.ResourceScopeSliding,
            absoluteExpiration: _cacheOptions.ResourceScopeAbsolute,
            cancellationToken: cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    // Epic → Project
    private async Task<(ScopeType, Guid)> ResolveEpicScopeAsync(
        string epicIdValue, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(epicIdValue, out var epicId))
            return (ScopeType.Project, Guid.Empty);

        var projectId = await _cache.GetOrCreateAsync(
            CacheKeys.EpicProject(epicId),
            () => _dbContext.Epics
                .AsNoTracking()
                .Where(e => e.Id == epicId)
                .Select(e => e.ProjectId)
                .FirstOrDefaultAsync(cancellationToken),
            slidingExpiration: _cacheOptions.ResourceScopeSliding,
            absoluteExpiration: _cacheOptions.ResourceScopeAbsolute,
            cancellationToken: cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    // Sprint → Project
    private async Task<(ScopeType, Guid)> ResolveSprintScopeAsync(
        string sprintIdValue, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sprintIdValue, out var sprintId))
            return (ScopeType.Project, Guid.Empty);

        var projectId = await _cache.GetOrCreateAsync(
            CacheKeys.SprintProject(sprintId),
            () => _dbContext.Sprints
                .AsNoTracking()
                .Where(s => s.Id == sprintId)
                .Select(s => s.ProjectId)
                .FirstOrDefaultAsync(cancellationToken),
            slidingExpiration: _cacheOptions.ResourceScopeSliding,
            absoluteExpiration: _cacheOptions.ResourceScopeAbsolute,
            cancellationToken: cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    // Comment → Project 

    private async Task<(ScopeType, Guid)> ResolveCommentScopeAsync(
        string commentIdValue, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(commentIdValue, out var commentId))
            return (ScopeType.Project, Guid.Empty);

        var projectId = await _cache.GetOrCreateAsync(
            CacheKeys.CommentProject(commentId),
            () => _dbContext.Comments
                .AsNoTracking()
                .Where(c => c.Id == commentId)
                .Select(c => c.Task.ProjectId)
                .FirstOrDefaultAsync(cancellationToken),
            slidingExpiration: _cacheOptions.ResourceScopeSliding,
            absoluteExpiration: _cacheOptions.ResourceScopeAbsolute,
            cancellationToken: cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    // Invitation → Scope 
    // Invitations have mutable state (Pending → Accepted/Revoked) but the
    // scope they belong to never changes, so we cache the scope mapping.

    private async Task<(ScopeType, Guid)> ResolveInvitationScopeAsync(
        string invitationIdValue, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(invitationIdValue, out var invitationId))
            return (ScopeType.Project, Guid.Empty);

        // Use a value-tuple wrapper since IMemoryCache stores object references
        var cached = await _cache.GetOrCreateAsync(
            $"cache:invitation-scope:{invitationId}",
            async () =>
            {
                var result = await _dbContext.Invitations
                    .AsNoTracking()
                    .Where(i => i.Id == invitationId)
                    .Select(i => new { i.ScopeType, i.ScopeId })
                    .FirstOrDefaultAsync(cancellationToken);

                return result is null
                    ? (ScopeType: ScopeType.Project, ScopeId: Guid.Empty)
                    : (result.ScopeType, result.ScopeId);
            },
            slidingExpiration: _cacheOptions.ResourceScopeSliding,
            absoluteExpiration: _cacheOptions.ResourceScopeAbsolute,
            cancellationToken: cancellationToken);

        return (cached.ScopeType, cached.ScopeId);
    }

    private static (ScopeType scopeType, Guid scopeId) ResolveGuidScope(string idValue, ScopeType scopeType)
        => Guid.TryParse(idValue, out var scopeId)
            ? (scopeType, scopeId)
            : (scopeType, Guid.Empty);
}
