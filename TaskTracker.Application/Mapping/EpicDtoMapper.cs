using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

internal static class EpicDtoMapper
{
    public static EpicDto ToDto(Epic epic)
    {
        return new EpicDto
        {
            Id = epic.Id,
            ProjectId = epic.ProjectId,
            Title = epic.Title,
            Description = epic.Description,
            Status = epic.Status,
            CreatedAt = epic.CreatedAt,
            UpdatedAt = epic.UpdatedAt
        };
    }

    public static Expression<Func<Epic, EpicDto>> Projection()
    {
        return epic => new EpicDto
        {
            Id = epic.Id,
            ProjectId = epic.ProjectId,
            Title = epic.Title,
            Description = epic.Description,
            Status = epic.Status,
            CreatedAt = epic.CreatedAt,
            UpdatedAt = epic.UpdatedAt
        };
    }
}
