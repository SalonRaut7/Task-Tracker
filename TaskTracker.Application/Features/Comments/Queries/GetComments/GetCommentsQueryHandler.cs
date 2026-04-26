using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _commentRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;

    public GetCommentsQueryHandler(
        ICommentRepository commentRepository,
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository)
    {
        _commentRepository = commentRepository;
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var query = _commentRepository.Query();

        if (!_currentUser.IsSuperAdmin)
        {
            var userId = _currentUser.UserId;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException("Authentication is required.");
            }

            var organizationIds = await _membershipRepository.GetUserOrganizationIdsAsync(userId, cancellationToken);
            var projectIds = await _membershipRepository.GetUserProjectIdsAsync(userId, cancellationToken);

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
