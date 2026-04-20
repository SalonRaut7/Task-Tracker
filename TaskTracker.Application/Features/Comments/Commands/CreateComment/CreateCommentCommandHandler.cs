using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Commands.CreateComment;

public sealed class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserResourceAccessService _resourceAccessService;

    public CreateCommentCommandHandler(
        ICommentRepository commentRepository,
        ICurrentUserService currentUser,
        IUserResourceAccessService resourceAccessService)
    {
        _commentRepository = commentRepository;
        _currentUser = currentUser;
        _resourceAccessService = resourceAccessService;
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

        if (!_currentUser.Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            var canAccessTask = await _resourceAccessService.CanAccessTaskAsync(userId, request.TaskId, cancellationToken);
            if (!canAccessTask)
            {
                throw new ForbiddenAccessException($"No access to task '{request.TaskId}'.");
            }
        }

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

        var author = await _commentRepository.GetAuthorNameAsync(userId, cancellationToken);
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
