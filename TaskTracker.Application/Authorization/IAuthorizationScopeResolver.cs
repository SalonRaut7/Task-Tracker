using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.Authorization;

public interface IAuthorizationScopeResolver
{
    Task<(ScopeType scopeType, Guid scopeId)> ResolveScopeAsync(
        ResourceScope resourceScope,
        CancellationToken cancellationToken = default);
}
