using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Commands.DeleteComment;

public sealed class DeleteCommentCommandHandler : IRequestHandler<DeleteCommentCommand, bool>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly INotificationPushService _pushService;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator,
        INotificationPushService pushService)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
        _pushService = pushService;
    }

    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (comment is null)
        {
            return false;
        }

        // Check ownership: user must be the author or have higher role
        if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.UserId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var userId = _currentUser.UserId!;
        var isAuthor = string.Equals(comment.AuthorId, userId, StringComparison.Ordinal);

        // SuperAdmin can always moderate comments.
        if (_currentUser.IsSuperAdmin)
        {
            isAuthor = true;
        }
        
        if (!isAuthor)
        {
            // Not the author: check if user has higher role in the project
            var commentTask = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken);
            if (commentTask is null)
            {
                return false;
            }

            var userRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                userId, Domain.Enums.ScopeType.Project, commentTask.ProjectId, cancellationToken);
            var authorRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                comment.AuthorId, Domain.Enums.ScopeType.Project, commentTask.ProjectId, cancellationToken);

            if (!CommentModerationHelper.CanModerateComment(userRole, authorRole))
            {
                throw new ForbiddenAccessException("You do not have permission to delete this comment.");
            }
        }

        var commentTaskForBroadcast = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken);

        await _commentRepository.DeleteAsync(comment, cancellationToken);

        if (commentTaskForBroadcast is not null)
        {
            await _pushService.BroadcastTaskCommentsChangedAsync(
                commentTaskForBroadcast.ProjectId,
                commentTaskForBroadcast.Id,
                cancellationToken);
        }
        return true;
    }
}
