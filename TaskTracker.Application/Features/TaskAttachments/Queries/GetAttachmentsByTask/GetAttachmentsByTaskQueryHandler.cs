using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.TaskAttachments.Queries.GetAttachmentsByTask;

public class GetAttachmentsByTaskQueryHandler
    : IRequestHandler<GetAttachmentsByTaskQuery, IReadOnlyList<TaskAttachmentDto>>
{
    private readonly ITaskAttachmentRepository _attachmentRepository;

    public GetAttachmentsByTaskQueryHandler(ITaskAttachmentRepository attachmentRepository)
    {
        _attachmentRepository = attachmentRepository;
    }

    public async Task<IReadOnlyList<TaskAttachmentDto>> Handle(
        GetAttachmentsByTaskQuery query,
        CancellationToken cancellationToken)
    {
        var taskExists = await _attachmentRepository.TaskExistsAsync(query.TaskId, cancellationToken);
        if (!taskExists)
            throw new KeyNotFoundException($"Task '{query.TaskId}' was not found.");

        var attachments = await _attachmentRepository.GetByTaskIdAsync(query.TaskId, cancellationToken);

        return attachments.Select(TaskAttachmentDtoMapper.ToDto).ToList();
    }
}
