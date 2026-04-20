namespace TaskTracker.Application.DTOs;

public sealed class CreateCommentDto
{
    public int TaskId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class UpdateCommentDto
{
    public string Content { get; set; } = string.Empty;
}

public sealed class CommentDto
{
    public Guid Id { get; set; }
    public int TaskId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
