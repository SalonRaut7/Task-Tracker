using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Domain.Entities;

public class TaskAttachment
{
    public Guid Id { get; private set; }
    public int TaskId { get; private set; }
    public string UploaderId { get; private set; } = string.Empty;

    // File metadata
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }

    // Cloudinary identifiers
    public string CloudinaryPublicId { get; private set; } = string.Empty;
    public string CloudinaryUrl { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty; // "image" or "raw"

    public DateTime CreatedAt { get; private set; }

    // Navigation
    public TaskItem Task { get; private set; } = null!;
    public ApplicationUser Uploader { get; private set; } = null!;

    // Required by EF Core — not for external use
    private TaskAttachment() { }

    public static TaskAttachment Create(
        int taskId,
        string uploaderId,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string cloudinaryPublicId,
        string cloudinaryUrl,
        string resourceType,
        DateTime utcNow)
    {
        if (taskId <= 0)
            throw new InvalidOperationException("TaskId must be a positive integer.");
        if (string.IsNullOrWhiteSpace(uploaderId))
            throw new InvalidOperationException("UploaderId is required.");
        if (string.IsNullOrWhiteSpace(fileName))
            throw new InvalidOperationException("FileName is required.");
        if (string.IsNullOrWhiteSpace(cloudinaryPublicId))
            throw new InvalidOperationException("CloudinaryPublicId is required.");
        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
            throw new InvalidOperationException("CloudinaryUrl is required.");
        if (fileSizeBytes <= 0)
            throw new InvalidOperationException("FileSizeBytes must be positive.");

        return new TaskAttachment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UploaderId = uploaderId.Trim(),
            FileName = fileName.Trim(),
            ContentType = contentType?.Trim() ?? string.Empty,
            FileSizeBytes = fileSizeBytes,
            CloudinaryPublicId = cloudinaryPublicId.Trim(),
            CloudinaryUrl = cloudinaryUrl.Trim(),
            ResourceType = resourceType?.Trim() ?? "raw",
            CreatedAt = utcNow.Kind == DateTimeKind.Utc ? utcNow : utcNow.ToUniversalTime(),
        };
    }
}
