using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.CancelSprint;

public sealed class CancelSprintCommandHandler : IRequestHandler<CancelSprintCommand, SprintDto>
{
    private readonly ISprintRepository _sprintRepository;

    public CancelSprintCommandHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<SprintDto> Handle(CancelSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdWithTasksAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Sprint not found.");

        if (sprint.Status is not (SprintStatus.Planning or SprintStatus.Active))
            throw new InvalidOperationException(
                $"Only a Planning or Active sprint can be cancelled. Current status: {sprint.Status}.");

        var utcNow = DateTime.UtcNow;
        var today  = DateOnly.FromDateTime(utcNow);

        // Unlink all tasks — they return to the backlog
        foreach (var task in sprint.Tasks)
            task.UnlinkFromSprint(utcNow);

        sprint.TransitionTo(SprintStatus.Cancelled, today, utcNow);

        await _sprintRepository.UpdateAsync(sprint, cancellationToken);

        return SprintDtoMapper.ToDto(sprint);
    }
}
