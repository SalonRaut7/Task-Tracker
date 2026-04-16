namespace TaskTracker.Application.Authorization;

public interface IAuthorizedRequest
{
    string RequiredPermission { get; }
    IReadOnlyList<ResourceScope> Scopes { get; }
}