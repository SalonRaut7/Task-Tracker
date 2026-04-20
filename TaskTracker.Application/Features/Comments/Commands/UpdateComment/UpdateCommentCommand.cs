using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Comments.Commands.UpdateComment;

public sealed class UpdateCommentCommand : IRequest<CommentDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.CommentsUpdate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Comment, Id)];
}
