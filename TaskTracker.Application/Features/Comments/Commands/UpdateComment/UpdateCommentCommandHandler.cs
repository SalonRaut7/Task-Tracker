using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Commands.UpdateComment;

public sealed class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, CommentDto?>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public UpdateCommentCommandHandler(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUser,
        IPermissionEvaluator permissionEvaluator)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _currentUser = currentUser;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<CommentDto?> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _commentRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (comment is null)
        {
            return null;
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
            var task = await _taskRepository.GetByIdAsync(comment.TaskId, cancellationToken);
            if (task is null)
            {
                throw new ForbiddenAccessException("You do not have permission to edit this comment.");
            }

            var userRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                userId, Domain.Enums.ScopeType.Project, task.ProjectId, cancellationToken);
            var authorRole = await _permissionEvaluator.GetUserRoleInScopeAsync(
                comment.AuthorId, Domain.Enums.ScopeType.Project, task.ProjectId, cancellationToken);

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

        return new CommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorId = comment.AuthorId,
            AuthorName = string.Concat(author.Value.FirstName, " ", author.Value.LastName).Trim(),
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }
}
