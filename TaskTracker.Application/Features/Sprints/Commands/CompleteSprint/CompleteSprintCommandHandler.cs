using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.CompleteSprint;

public sealed class CompleteSprintCommandHandler : IRequestHandler<CompleteSprintCommand, CompleteSprintResult>
{
    private readonly ISprintRepository _sprintRepository;

    public CompleteSprintCommandHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<CompleteSprintResult> Handle(CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdWithTasksAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Sprint not found.");

        if (sprint.Status != SprintStatus.Active)
            throw new InvalidOperationException(
                $"Only an Active sprint can be completed. Current status: {sprint.Status}.");

        var utcNow = DateTime.UtcNow;
        var today  = DateOnly.FromDateTime(utcNow);

        // Count and roll over incomplete tasks to the backlog
        var terminalStatuses = new[] { Status.Completed, Status.Cancelled };
        var incompleteTasks  = sprint.Tasks.Where(t => !terminalStatuses.Contains(t.Status)).ToList();

        foreach (var task in incompleteTasks)
            task.UnlinkFromSprint(utcNow);

        // Transition sprint status (after collecting tasks so Tasks collection is still loaded)
        sprint.TransitionTo(SprintStatus.Completed, today, utcNow);

        await _sprintRepository.UpdateAsync(sprint, cancellationToken);

        return new CompleteSprintResult
        {
            Sprint             = SprintDtoMapper.ToDto(sprint),
            IncompleteTaskCount = incompleteTasks.Count,
            RolledOverTaskCount = incompleteTasks.Count,
        };
    }
}
