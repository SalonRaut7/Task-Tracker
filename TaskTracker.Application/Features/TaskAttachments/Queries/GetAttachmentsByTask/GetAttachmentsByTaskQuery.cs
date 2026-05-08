using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.TaskAttachments.Queries.GetAttachmentsByTask;

public class GetAttachmentsByTaskQuery : IRequest<IReadOnlyList<TaskAttachmentDto>>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksView;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, ProjectId)];

    /// Resolved by the controller before sending to MediatR using GetProjectIdByTaskIdAsync.
    public Guid ProjectId { get; set; }

    public int TaskId { get; set; }
}
