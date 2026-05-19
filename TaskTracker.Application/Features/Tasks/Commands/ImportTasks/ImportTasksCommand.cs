using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Features.Tasks.Commands.ImportTasks;

public class ImportTasksCommand : IRequest<TaskImportResultDto>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksCreate;
    public IReadOnlyList<ResourceScope> Scopes =>
        [new ResourceScope(ResourceType.Project, ProjectId)];

    public Guid ProjectId { get; set; }
    public byte[] FileBytes { get; set; } = [];
}
