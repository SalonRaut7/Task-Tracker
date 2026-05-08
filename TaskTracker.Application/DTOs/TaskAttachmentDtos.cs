namespace TaskTracker.Application.DTOs;

public class TaskAttachmentDto
{
    public Guid Id { get; set; }
    public int TaskId { get; set; }
    public string UploaderId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
