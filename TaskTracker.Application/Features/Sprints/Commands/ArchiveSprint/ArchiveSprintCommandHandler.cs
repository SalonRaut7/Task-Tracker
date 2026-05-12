using MediatR;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Mapping;
using TaskTracker.Domain.Enums;
using TaskTracker.Domain.Interfaces;

namespace TaskTracker.Application.Features.Sprints.Commands.ArchiveSprint;

public sealed class ArchiveSprintCommandHandler : IRequestHandler<ArchiveSprintCommand, SprintDto>
{
    private readonly ISprintRepository _sprintRepository;
    private readonly ICurrentUserService _currentUser;

    public ArchiveSprintCommandHandler(ISprintRepository sprintRepository, ICurrentUserService currentUser)
    {
        _sprintRepository = sprintRepository;
        _currentUser = currentUser;
    }

    public async Task<SprintDto> Handle(ArchiveSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _sprintRepository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Sprint not found.");

        if (sprint.Status is not (SprintStatus.Completed or SprintStatus.Cancelled))
            throw new InvalidOperationException(
                $"Only a Completed or Cancelled sprint can be archived. Current status: {sprint.Status}.");

        var currentUserId = _currentUser.UserId ?? throw new InvalidOperationException("Authenticated user ID could not be resolved");

        await _sprintRepository.ArchiveAsync(sprint, currentUserId, request.ArchiveReason, cancellationToken);

        return SprintDtoMapper.ToDto(sprint);
    }
}
