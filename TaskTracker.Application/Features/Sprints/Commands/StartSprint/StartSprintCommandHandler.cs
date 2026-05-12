using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.StartSprint;

public sealed class StartSprintCommandHandler : IRequestHandler<StartSprintCommand, SprintDto>
{
    private readonly ISprintRepository _sprintRepository;

    public StartSprintCommandHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<SprintDto> Handle(StartSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdWithTasksAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Sprint not found.");

        if (sprint.Status != SprintStatus.Planning)
            throw new InvalidOperationException(
                $"Only a Planning sprint can be started. Current status: {sprint.Status}.");

        // Guard: sprint must have at least one task
        if (!sprint.Tasks.Any())
            throw new InvalidOperationException(
                "Cannot start a sprint with no tasks. Assign at least one task before starting.");

        // Guard: no other Active sprint for the same project
        var alreadyActive = await _sprintRepository.HasActiveSprintAsync(
            sprint.ProjectId, excludeSprintId: sprint.Id, cancellationToken: cancellationToken);

        if (alreadyActive)
            throw new InvalidOperationException(
                "Another sprint is already active for this project. Complete or cancel it before starting a new one.");

        // Guard: no date overlap with other sprints
        var overlaps = await _sprintRepository.HasOverlappingSprintAsync(
            sprint.ProjectId, sprint.StartDate, sprint.EndDate,
            excludeSprintId: sprint.Id, cancellationToken: cancellationToken);

        if (overlaps)
            throw new InvalidOperationException(
                "The sprint's date range overlaps with another sprint in this project.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Warn via exception if start date has already passed (block activation)
        if (sprint.StartDate < today)
            throw new InvalidOperationException(
                $"Cannot start a sprint whose StartDate ({sprint.StartDate}) is in the past. " +
                "Update the StartDate to today or a future date before starting.");

        sprint.TransitionTo(SprintStatus.Active, today, DateTime.UtcNow);

        await _sprintRepository.UpdateAsync(sprint, cancellationToken);

        return SprintDtoMapper.ToDto(sprint);
    }
}
