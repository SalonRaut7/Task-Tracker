using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs;

public abstract class EpicBaseDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Status Status { get; set; } = Status.NotStarted;
}

public sealed class CreateEpicDto : EpicBaseDto
{
    public Guid ProjectId { get; set; }
}

public sealed class UpdateEpicDto : EpicBaseDto;

public sealed class EpicDto : EpicBaseDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
