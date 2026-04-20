using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Comments.Queries.GetCommentById;

public sealed class GetCommentByIdQuery : IRequest<CommentDto?>, IAuthorizedRequest
{
    public Guid Id { get; set; }

    public string RequiredPermission => AppPermissions.CommentsView;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Comment, Id)];
}
