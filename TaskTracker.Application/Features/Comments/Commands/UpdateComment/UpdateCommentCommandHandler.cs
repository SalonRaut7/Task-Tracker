using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Commands.UpdateComment;

public sealed class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto?>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly INotificationPushService _pushService;

    public UpdateCommentCommandHandler(
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

    public async Task<CommentDto?> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (comment is null)
        {
            return null;
        }

        // Check ownership: user must be the author or have higher role
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
                throw new ForbiddenAccessException("You do not have permission to edit this comment.");
            }

            var userRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                userId, Domain.Enums.ScopeType.Project, commentTask.ProjectId, cancellationToken);
            var authorRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                comment.AuthorId, Domain.Enums.ScopeType.Project, commentTask.ProjectId, cancellationToken);

            if (!CommentModerationHelper.CanModerateComment(userRole, authorRole))
            {
                throw new ForbiddenAccessException("You do not have permission to edit this comment.");
            }
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment, cancellationToken);

        var author = await _commentRepository.GetAuthorNameAsync(comment.AuthorId, cancellationToken);
        if (!author.HasValue)
        {
            throw new InvalidOperationException("Author details were not found.");
        }

        var dto = CommentDtoMapper.ToDto(
            comment,
            string.Concat(author.Value.FirstName, " ", author.Value.LastName).Trim());

        var commentTaskForBroadcast = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken);
        if (commentTaskForBroadcast is not null)
        {
            await _pushService.BroadcastTaskCommentsChangedAsync(
                commentTaskForBroadcast.ProjectId,
                commentTaskForBroadcast.Id,
                cancellationToken);
        }

        return dto;
    }
}
