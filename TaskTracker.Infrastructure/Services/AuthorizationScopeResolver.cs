using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Services;

public sealed class AuthorizationScopeResolver : IAuthorizationScopeResolver
{
    private readonly AppDbContext _dbContext;

    public AuthorizationScopeResolver(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

    private async Task<(ScopeType scopeType, Guid scopeId)> ResolveTaskScopeAsync(
        string taskIdValue,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(taskIdValue, out var taskId))
        {
            return (ScopeType.Project, Guid.Empty);
        }

        var projectId = await _dbContext.Tasks
            .AsNoTracking()
            .Where(task => task.Id == taskId)
            .Select(task => task.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    private async Task<(ScopeType scopeType, Guid scopeId)> ResolveEpicScopeAsync(
        string epicIdValue,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(epicIdValue, out var epicId))
        {
            return (ScopeType.Project, Guid.Empty);
        }

        var projectId = await _dbContext.Epics
            .AsNoTracking()
            .Where(epic => epic.Id == epicId)
            .Select(epic => epic.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    private async Task<(ScopeType scopeType, Guid scopeId)> ResolveSprintScopeAsync(
        string sprintIdValue,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sprintIdValue, out var sprintId))
        {
            return (ScopeType.Project, Guid.Empty);
        }

        var projectId = await _dbContext.Sprints
            .AsNoTracking()
            .Where(sprint => sprint.Id == sprintId)
            .Select(sprint => sprint.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    private async Task<(ScopeType scopeType, Guid scopeId)> ResolveCommentScopeAsync(
        string commentIdValue,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(commentIdValue, out var commentId))
        {
            return (ScopeType.Project, Guid.Empty);
        }

        var projectId = await _dbContext.Comments
            .AsNoTracking()
            .Where(comment => comment.Id == commentId)
            .Select(comment => comment.Task.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId == Guid.Empty
            ? (ScopeType.Project, Guid.Empty)
            : (ScopeType.Project, projectId);
    }

    private async Task<(ScopeType scopeType, Guid scopeId)> ResolveInvitationScopeAsync(
        string invitationIdValue,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(invitationIdValue, out var invitationId))
        {
            return (ScopeType.Project, Guid.Empty);
        }

        var invitationScope = await _dbContext.Invitations
            .AsNoTracking()
            .Where(invitation => invitation.Id == invitationId)
            .Select(invitation => new { invitation.ScopeType, invitation.ScopeId })
            .FirstOrDefaultAsync(cancellationToken);

        return invitationScope is null
            ? (ScopeType.Project, Guid.Empty)
            : (invitationScope.ScopeType, invitationScope.ScopeId);
    }

    private static (ScopeType scopeType, Guid scopeId) ResolveGuidScope(string idValue, ScopeType scopeType)
    {
        return Guid.TryParse(idValue, out var scopeId)
            ? (scopeType, scopeId)
            : (scopeType, Guid.Empty);
    }
}
