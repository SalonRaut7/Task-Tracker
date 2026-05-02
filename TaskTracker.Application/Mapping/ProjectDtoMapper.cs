using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

internal static class ProjectDtoMapper
{
    public static ProjectDto ToDto(Project project)
    {
        return new ProjectDto
        {
            Id = project.Id,
            OrganizationId = project.OrganizationId,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    public static Expression<Func<Project, ProjectDto>> Projection()
    {
        return project => new ProjectDto
        {
            Id = project.Id,
            OrganizationId = project.OrganizationId,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }
}
