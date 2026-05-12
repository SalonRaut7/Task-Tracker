using MediatR;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.DeleteSprint;

public sealed class DeleteSprintCommandHandler : IRequestHandler<DeleteSprintCommand, bool>
{
    private readonly ISprintRepository _sprintRepository;

    public DeleteSprintCommandHandler(ISprintRepository sprintRepository)
    {
        _sprintRepository = sprintRepository;
    }

    public async Task<bool> Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (sprint is null)
            return false;

        // Only Planning or Cancelled sprints may be deleted.
        // Active → must cancel first. Completed/Archived → use Archive for history.
        if (sprint.Status is not (SprintStatus.Planning or SprintStatus.Cancelled))
            throw new InvalidOperationException(
                sprint.Status == SprintStatus.Active
                    ? "An Active sprint must be cancelled before it can be deleted."
                    : $"A {sprint.Status} sprint cannot be deleted.");

        await _sprintRepository.DeleteAsync(sprint, cancellationToken);
        return true;
    }
}
