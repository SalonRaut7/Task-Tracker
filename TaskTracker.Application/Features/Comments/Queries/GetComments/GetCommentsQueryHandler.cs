using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUserResourceAccessService _resourceAccessService;

    public GetCommentsQueryHandler(
        ICommentRepository commentRepository,
        ICurrentUserService currentUser,
        IUserResourceAccessService resourceAccessService)
    {
        _commentRepository = commentRepository;
        _currentUser = currentUser;
        _resourceAccessService = resourceAccessService;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = _commentRepository.Query();

        if (!_currentUser.Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Authentication is required.");
            }

            if (request.TaskId.HasValue)
            {
                var canAccessTask = await _resourceAccessService.CanAccessTaskAsync(
                    userId,
                    request.TaskId.Value,
                    cancellationToken);

                if (!canAccessTask)
                {
                    throw new ForbiddenAccessException($"No access to task '{request.TaskId.Value}'.");
                }
            }

            var organizationIds = await _resourceAccessService.GetUserOrganizationIdsAsync(userId, cancellationToken);
            var projectIds = await _resourceAccessService.GetUserProjectIdsAsync(userId, cancellationToken);

            if (organizationIds.Count == 0 || projectIds.Count == 0)
            {
                return [];
            }

            query = query.Where(comment =>
                projectIds.Contains(comment.Task.ProjectId)
                && organizationIds.Contains(comment.Task.Project.OrganizationId));
        }

        if (request.TaskId.HasValue)
        {
            query = query.Where(comment => comment.TaskId == request.TaskId.Value);
        }

        return await query
            .OrderByDescending(comment => comment.CreatedAt)
            .Select(comment => new CommentDto
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                AuthorId = comment.AuthorId,
                AuthorName = string.Concat(comment.Author.FirstName, " ", comment.Author.LastName).Trim(),
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
