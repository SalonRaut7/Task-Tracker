using MediatR;
using TaskTracker.Application.DTOs;
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
        {
            return null;
        }

        sprint.Name = request.Name.Trim();
        sprint.Goal = string.IsNullOrWhiteSpace(request.Goal) ? null : request.Goal.Trim();
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.Status = request.Status;
        sprint.UpdatedAt = DateTime.UtcNow;

        await _sprintRepository.UpdateAsync(sprint, cancellationToken);

        return new SprintDto
        {
            Id = sprint.Id,
            ProjectId = sprint.ProjectId,
            Name = sprint.Name,
            Goal = sprint.Goal,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Status = sprint.Status,
            CreatedAt = sprint.CreatedAt,
            UpdatedAt = sprint.UpdatedAt
        };
    }
}
