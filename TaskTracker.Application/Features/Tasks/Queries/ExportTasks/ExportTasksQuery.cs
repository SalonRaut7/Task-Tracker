using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Queries.ExportTasks;

/// Result returned by the export handler — includes the project key for naming the file.
public record ExportTasksResult(byte[] FileBytes, string ProjectKey);

public class ExportTasksQuery : IRequest<ExportTasksResult>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksView;
    public IReadOnlyList<ResourceScope> Scopes =>
        [new ResourceScope(ResourceType.Project, ProjectId)];

    public Guid ProjectId { get; set; }
    public bool BacklogOnly { get; set; }
}
