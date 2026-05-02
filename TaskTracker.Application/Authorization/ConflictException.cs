namespace TaskTracker.Application.Authorization;

public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
