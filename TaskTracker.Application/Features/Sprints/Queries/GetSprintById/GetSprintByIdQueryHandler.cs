using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Queries.GetSprintById;

public sealed class GetSprintByIdQueryHandler : IRequestHandler<GetSprintByIdQuery, SprintDto?>
{
    private readonly ISprintRepository _sprintRepository;

    public GetSprintByIdQueryHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<SprintDto?> Handle(GetSprintByIdQuery request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdAsync(request.Id, cancellationToken);
        if (sprint is null)
        {
            return null;
        }

        return SprintDtoMapper.ToDto(sprint);
    }
}
