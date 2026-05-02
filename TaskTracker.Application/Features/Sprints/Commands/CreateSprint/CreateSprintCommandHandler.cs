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
        var now = DateTime.UtcNow;
        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            Goal = string.IsNullOrWhiteSpace(request.Goal) ? null : request.Goal.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _sprintRepository.AddAsync(sprint, cancellationToken);

        return SprintDtoMapper.ToDto(sprint);
    }
}
