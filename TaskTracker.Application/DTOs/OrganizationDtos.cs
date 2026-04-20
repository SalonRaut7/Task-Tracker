namespace TaskTracker.Application.DTOs;

public abstract class OrganizationBaseDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class CreateOrganizationDto : OrganizationBaseDto;

public sealed class UpdateOrganizationDto : OrganizationBaseDto;

public sealed class OrganizationDto : OrganizationBaseDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
