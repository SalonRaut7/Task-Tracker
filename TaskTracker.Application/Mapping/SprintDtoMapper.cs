using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

internal static class SprintDtoMapper
{
    public static SprintDto ToDto(Sprint sprint)
    {
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

    public static Expression<Func<Sprint, SprintDto>> Projection()
    {
        return sprint => new SprintDto
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
