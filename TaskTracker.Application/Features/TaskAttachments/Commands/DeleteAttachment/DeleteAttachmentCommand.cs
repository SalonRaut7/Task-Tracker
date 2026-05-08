using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.TaskAttachments.Commands.DeleteAttachment;

public class DeleteAttachmentCommand : IRequest<Unit>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksUpdate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, ProjectId)];

    /// Resolved by the controller before sending to MediatR.
    public Guid ProjectId { get; set; }

    public Guid Id { get; set; }
    public int TaskId { get; set; }
}
