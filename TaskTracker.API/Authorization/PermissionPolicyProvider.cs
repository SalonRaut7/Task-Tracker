using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TaskTracker.Domain.Constants;

namespace TaskTracker.API.Authorization;

/// Dynamically creates authorization policies from permission strings.
/// Any [Authorize(Policy = "Tasks.Create")] will automatically get a PermissionRequirement.

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;
    private readonly HashSet<string> _validPermissions;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
        _validPermissions = AppPermissions.GetAllPermissions().ToHashSet();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (_validPermissions.Contains(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
