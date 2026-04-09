namespace TaskTracker.Application.DTOs
{
    public class PagedResultDto<T>
    {
        public IReadOnlyList<T> Data { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
    }
}