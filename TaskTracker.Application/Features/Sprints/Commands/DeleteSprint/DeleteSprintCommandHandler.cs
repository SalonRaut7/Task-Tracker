using MediatR;
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
        {
            return false;
        }

        await _sprintRepository.DeleteAsync(sprint, cancellationToken);
        return true;
    }
}
