using System.Linq.Expressions;
using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Mapping;

internal static class OrganizationDtoMapper
{
    public static OrganizationDto ToDto(Organization organization)
    {
        return new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }

    public static Expression<Func<Organization, OrganizationDto>> Projection()
    {
        return organization => new OrganizationDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }
}
