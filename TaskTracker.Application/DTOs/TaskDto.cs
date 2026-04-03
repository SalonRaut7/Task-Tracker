using TaskTracker.Domain.Enums;
//DTO for getting task data from the database and sending it to the client
namespace TaskTracker.Application.DTOs
{
    public class TaskDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Status Status { get; set; }
    }
}