using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Features.TaskAttachments.Commands.DeleteAttachment;
using TaskTracker.Application.Features.TaskAttachments.Commands.UploadAttachment;
using TaskTracker.Application.Features.TaskAttachments.Queries.GetAttachmentsByTask;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskAttachmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITaskAttachmentRepository _attachmentRepository;

    public TaskAttachmentsController(
        IMediator mediator,
        ITaskAttachmentRepository attachmentRepository)
    {
        _mediator = mediator;
        _attachmentRepository = attachmentRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskAttachmentDto>>> GetByTask(
        [FromQuery] int taskId,
        CancellationToken cancellationToken)
    {
        var projectId = await _attachmentRepository.GetProjectIdByTaskIdAsync(taskId, cancellationToken);
        if (!projectId.HasValue)
            return NotFound($"Task '{taskId}' was not found.");

        var query = new GetAttachmentsByTaskQuery
        {
            TaskId = taskId,
            ProjectId = projectId.Value,
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)] // Slightly above 10 MB to account for multipart overhead
    public async Task<ActionResult<TaskAttachmentDto>> Upload(
        [FromForm] int taskId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file was provided.");

        var projectId = await _attachmentRepository.GetProjectIdByTaskIdAsync(taskId, cancellationToken);
        if (!projectId.HasValue)
            return NotFound($"Task '{taskId}' was not found.");

        // Read file into byte[] at the controller boundary
        byte[] fileData;
        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            fileData = memoryStream.ToArray();
        }

        var command = new UploadAttachmentCommand
        {
            TaskId = taskId,
            ProjectId = projectId.Value,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            FileData = fileData,
        };

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetByTask), new { taskId }, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        [FromRoute] Guid id,
        [FromQuery] int taskId,
        CancellationToken cancellationToken)
    {
        var projectId = await _attachmentRepository.GetProjectIdByTaskIdAsync(taskId, cancellationToken);
        if (!projectId.HasValue)
            return NotFound($"Task '{taskId}' was not found.");

        var command = new DeleteAttachmentCommand
        {
            Id = id,
            TaskId = taskId,
            ProjectId = projectId.Value,
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/download")]
    public async Task<ActionResult> Download(
        [FromRoute] Guid id,
        [FromQuery] int taskId,
        CancellationToken cancellationToken)
    {
        var projectId = await _attachmentRepository.GetProjectIdByTaskIdAsync(taskId, cancellationToken);
        if (!projectId.HasValue)
            return NotFound($"Task '{taskId}' was not found.");

        var attachment = await _attachmentRepository.GetByIdAsync(id, cancellationToken);
        if (attachment is null || attachment.TaskId != taskId)
            return NotFound($"Attachment '{id}' was not found.");

        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(
            attachment.CloudinaryUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "Failed to download attachment.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = string.IsNullOrWhiteSpace(attachment.ContentType)
            ? "application/octet-stream"
            : attachment.ContentType;

        return File(bytes, contentType, attachment.FileName);
    }
}
