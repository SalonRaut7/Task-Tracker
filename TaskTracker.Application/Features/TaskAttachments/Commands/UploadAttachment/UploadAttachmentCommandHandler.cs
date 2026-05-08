using MediatR;
using Microsoft.Extensions.Logging;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.TaskAttachments.Commands.UploadAttachment;

public class UploadAttachmentCommandHandler : IRequestHandler<UploadAttachmentCommand, TaskAttachmentDto>
{
    private readonly ITaskAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UploadAttachmentCommandHandler> _logger;

    private const int MaxAttachmentsPerTask = 10;

    public UploadAttachmentCommandHandler(
        ITaskAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUser,
        ILogger<UploadAttachmentCommandHandler> logger)
    {
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<TaskAttachmentDto> Handle(
        UploadAttachmentCommand command,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.UserId!;

        // Verify task exists
        var taskExists = await _attachmentRepository.TaskExistsAsync(command.TaskId, cancellationToken);
        if (!taskExists)
            throw new KeyNotFoundException($"Task '{command.TaskId}' was not found.");

        // Enforce attachment cap
        var currentCount = await _attachmentRepository.CountByTaskIdAsync(command.TaskId, cancellationToken);
        if (currentCount >= MaxAttachmentsPerTask)
            throw new InvalidOperationException(
                $"Maximum of {MaxAttachmentsPerTask} attachments per task has been reached.");

        // Upload to Cloudinary
        var uploadResult = await _fileStorageService.UploadAsync(
            command.FileData,
            command.FileName,
            command.ContentType,
            cancellationToken);

        // Create domain entity
        var attachment = TaskAttachment.Create(
            command.TaskId,
            currentUserId,
            command.FileName,
            command.ContentType,
            command.FileSizeBytes,
            uploadResult.PublicId,
            uploadResult.SecureUrl,
            uploadResult.ResourceType,
            DateTime.UtcNow);

        // Save to database — compensate on failure
        try
        {
            await _attachmentRepository.AddAsync(attachment, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Database insert failed for attachment {FileName} on task {TaskId}. Deleting orphaned Cloudinary asset {PublicId}.",
                command.FileName, command.TaskId, uploadResult.PublicId);

            try
            {
                await _fileStorageService.DeleteAsync(uploadResult.PublicId, uploadResult.ResourceType, cancellationToken);
            }
            catch (Exception deleteEx)
            {
                _logger.LogError(deleteEx,
                    "Failed to delete orphaned Cloudinary asset {PublicId} after DB failure.",
                    uploadResult.PublicId);
            }

            throw;
        }

        return TaskAttachmentDtoMapper.ToDto(attachment);
    }
}
