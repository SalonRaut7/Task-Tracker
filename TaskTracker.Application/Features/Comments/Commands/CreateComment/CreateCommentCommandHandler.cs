using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Commands.CreateComment;

public sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationPushService _pushService;

    public CreateCommentCommandHandler(
        ICommentRepository commentRepository,
        ITaskRepository taskRepository,
        ICurrentUserService currentUser,
        INotificationPushService pushService)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
        _currentUser = currentUser;
        _pushService = pushService;
    }

    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authentication is required.");
        }

        var taskExists = await _commentRepository.TaskExistsAsync(request.TaskId, cancellationToken);
        if (!taskExists)
        {
            throw new KeyNotFoundException($"Task '{request.TaskId}' was not found.");
        }

        // Resource-level access is enforced by AuthorizationBehavior via IAuthorizedRequest.
        // No need for a redundant CanAccessTaskAsync check here.

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = request.TaskId,
            AuthorId = userId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _commentRepository.AddAsync(comment, cancellationToken);

        var task = await _taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            throw new InvalidOperationException("Task details were not found.");
        }

        var author = await _commentRepository.GetAuthorNameAsync(userId, cancellationToken);
        if (!author.HasValue)
        {
            throw new InvalidOperationException("Author details were not found.");
        }

        var dto = new CommentDto
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorId = comment.AuthorId,
            AuthorName = string.Concat(author.Value.FirstName, " ", author.Value.LastName).Trim(),
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };

        await _pushService.BroadcastTaskCommentsChangedAsync(task.ProjectId, task.Id, cancellationToken);

        return dto;
    }
}
