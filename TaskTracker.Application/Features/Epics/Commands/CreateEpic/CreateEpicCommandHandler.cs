using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Epics.Commands.CreateEpic;

public sealed class CreateEpicCommandHandler : IRequestHandler<CreateEpicCommand, EpicDto>
{
    private readonly IEpicRepository _epicRepository;

    public CreateEpicCommandHandler(IEpicRepository epicRepository)
    {
        _epicRepository = epicRepository;
    }

    public async Task<EpicDto> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var epic = new Epic
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _epicRepository.AddAsync(epic, cancellationToken);

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
