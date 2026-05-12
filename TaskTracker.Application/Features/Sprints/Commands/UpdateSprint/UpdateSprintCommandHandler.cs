using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.UpdateSprint;

public sealed class UpdateSprintCommandHandler : IRequestHandler<UpdateSprintCommand, SprintDto?>
{
    private readonly ISprintRepository _sprintRepository;

    public UpdateSprintCommandHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<SprintDto?> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (sprint is null)
            return null;

        // Guard: no date overlap with other sprints in this project (excluding self)
        var overlaps = await _sprintRepository.HasOverlappingSprintAsync(
            sprint.ProjectId, request.StartDate, request.EndDate,
            excludeSprintId: sprint.Id, cancellationToken: cancellationToken);

        if (overlaps)
            throw new InvalidOperationException(
                "The updated date range overlaps with another sprint in this project.");

        // Domain method enforces: no edits to closed sprints, StartDate locked when Active
        sprint.ApplyUpdate(
            request.Name,
            request.Goal,
            request.StartDate,
            request.EndDate,
            DateTime.UtcNow);

        await _sprintRepository.UpdateAsync(sprint, cancellationToken);

        return SprintDtoMapper.ToDto(sprint);
    }
}
