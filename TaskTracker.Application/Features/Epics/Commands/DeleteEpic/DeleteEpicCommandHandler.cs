using MediatR;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Epics.Commands.DeleteEpic;

public sealed class DeleteEpicCommandHandler : IRequestHandler<DeleteEpicCommand, bool>
{
    private readonly IEpicRepository _epicRepository;

    public DeleteEpicCommandHandler(IEpicRepository epicRepository)
    {
        _epicRepository = epicRepository;
    }

    public async Task<bool> Handle(DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _epicRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (epic is null)
        {
            return false;
        }

        await _epicRepository.DeleteAsync(epic, cancellationToken);
        return true;
    }
}
