using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Comments.Commands.DeleteComment;

public sealed class DeleteCommentCommand : IRequest<bool>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.CommentsDelete;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Comment, Id)];
}
