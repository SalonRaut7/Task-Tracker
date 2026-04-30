namespace TaskTracker.Application.DTOs;
public class NotificationDto
{
    public Guid Id { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public Guid? ProjectId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
