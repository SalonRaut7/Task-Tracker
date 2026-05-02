using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.Comments.Queries.GetComments;

public sealed class GetCommentsQuery : IRequest<IReadOnlyList<CommentDto>>, IAuthorizedRequest
{
    public int? TaskId { get; set; }

    public string RequiredPermission => AppPermissions.CommentsView;
    public IReadOnlyList<ResourceScope> Scopes =>
        TaskId.HasValue
            ? [new ResourceScope(ResourceType.Task, TaskId.Value)]
            : [];
}
