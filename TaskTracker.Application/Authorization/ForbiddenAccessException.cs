namespace TaskTracker.Application.Authorization;

public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message)
        : base(message)
    {
    }
}