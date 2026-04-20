using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Epics.Queries.GetEpicById;

public sealed class GetEpicByIdQueryHandler : IRequestHandler<GetEpicByIdQuery, EpicDto?>
{
    private readonly IEpicRepository _epicRepository;

    public GetEpicByIdQueryHandler(IEpicRepository epicRepository)
    {
        _epicRepository = epicRepository;
    }

    public async Task<EpicDto?> Handle(GetEpicByIdQuery request, CancellationToken cancellationToken)
    {
        var epic = await _epicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (epic is null)
        {
            return null;
        }

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
