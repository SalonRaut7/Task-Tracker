namespace TaskTracker.Application.DTOs;

public class TaskImportValidationErrorDto
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class TaskImportResultDto
{
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<TaskImportValidationErrorDto> Errors { get; set; } = [];
}
