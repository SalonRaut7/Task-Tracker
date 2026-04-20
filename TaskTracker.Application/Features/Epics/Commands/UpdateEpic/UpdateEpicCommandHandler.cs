using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Epics.Commands.UpdateEpic;

public sealed class UpdateEpicCommandHandler : IRequestHandler<UpdateEpicCommand, EpicDto?>
{
    private readonly IEpicRepository _epicRepository;

    public UpdateEpicCommandHandler(IEpicRepository epicRepository)
    {
        _epicRepository = epicRepository;
    }

    public async Task<EpicDto?> Handle(UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _epicRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (epic is null)
        {
            return null;
        }

        epic.Title = request.Title.Trim();
        epic.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        epic.Status = request.Status;
        epic.UpdatedAt = DateTime.UtcNow;

        await _epicRepository.UpdateAsync(epic, cancellationToken);

        return new EpicDto
        {
            Id = epic.Id,
            ProjectId = epic.ProjectId,
            Title = epic.Title,
            Description = epic.Description,
            Status = epic.Status,
            CreatedAt = epic.CreatedAt,
            UpdatedAt = epic.UpdatedAt
        };
    }
}
