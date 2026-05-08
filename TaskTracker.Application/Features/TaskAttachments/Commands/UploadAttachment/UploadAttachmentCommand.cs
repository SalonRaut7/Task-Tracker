using MediatR;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Features.TaskAttachments.Commands.UploadAttachment;

public class UploadAttachmentCommand : IRequest<TaskAttachmentDto>, IAuthorizedRequest
{
    public string RequiredPermission => AppPermissions.TasksCreate;
    public IReadOnlyList<ResourceScope> Scopes => [new ResourceScope(ResourceType.Project, ProjectId)];

    /// Resolved by the controller before sending to MediatR.
    /// Set from the repository lookup of TaskId → ProjectId.
    public Guid ProjectId { get; set; }

    public int TaskId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }

    /// File bytes read from IFormFile by the controller.
    /// Validators MUST NOT read or inspect this field.
    public byte[] FileData { get; set; } = Array.Empty<byte>();
}
