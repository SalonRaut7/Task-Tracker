using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Comments.Commands.CreateComment;

public sealed class CreateCommentCommand : IRequest<CommentDto>, IAuthorizedRequest
{
    public int TaskId { get; set; }
    public string Content { get; set; } = string.Empty;

    public string RequiredPermission => AppPermissions.CommentsAdd;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Task, TaskId)];
}
