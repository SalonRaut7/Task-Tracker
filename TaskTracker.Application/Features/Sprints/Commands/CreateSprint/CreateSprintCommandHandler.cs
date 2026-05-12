using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.CreateSprint;

public sealed class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, SprintDto>
{
    private readonly ISprintRepository _sprintRepository;

    public CreateSprintCommandHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<SprintDto> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        // Guard: no date overlap with other sprints in this project
        var overlaps = await _sprintRepository.HasOverlappingSprintAsync(
            request.ProjectId, request.StartDate, request.EndDate,
            cancellationToken: cancellationToken);

        if (overlaps)
            throw new InvalidOperationException(
                "The sprint's date range overlaps with another sprint in this project.");

        var sprint = Sprint.Create(
            request.ProjectId,
            request.Name,
            request.Goal,
            request.StartDate,
            request.EndDate,
            DateTime.UtcNow);

        await _sprintRepository.AddAsync(sprint, cancellationToken);

        return SprintDtoMapper.ToDto(sprint);
    }
}
