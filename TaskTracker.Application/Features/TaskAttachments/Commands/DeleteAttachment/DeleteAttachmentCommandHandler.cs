using MediatR;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.TaskAttachments.Commands.DeleteAttachment;

public class DeleteAttachmentCommandHandler : IRequestHandler<DeleteAttachmentCommand, Unit>
{
    private readonly ITaskAttachmentRepository _attachmentRepository;
    private readonly IFileStorageService _fileStorageService;

    public DeleteAttachmentCommandHandler(
        ITaskAttachmentRepository attachmentRepository,
        IFileStorageService fileStorageService)
    {
        _attachmentRepository = attachmentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Unit> Handle(
        DeleteAttachmentCommand command,
        CancellationToken cancellationToken)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(command.Id, cancellationToken);
        if (attachment is null)
            throw new KeyNotFoundException($"Attachment '{command.Id}' was not found.");

        if (attachment.TaskId != command.TaskId)
            throw new InvalidOperationException("Attachment does not belong to the specified task.");

        // Delete from Cloudinary first
        await _fileStorageService.DeleteAsync(
            attachment.CloudinaryPublicId,
            attachment.ResourceType,
            cancellationToken);

        // Delete from database
        await _attachmentRepository.DeleteAsync(attachment, cancellationToken);

        return Unit.Value;
    }
}
