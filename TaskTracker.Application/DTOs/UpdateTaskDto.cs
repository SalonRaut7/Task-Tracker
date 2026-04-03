using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs
{
    public class UpdateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Status Status { get; set; }
    }
}