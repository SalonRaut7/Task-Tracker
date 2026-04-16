using Microsoft.AspNetCore.Authorization;

namespace TaskTracker.API.Authorization;
/// Represents a permission-based authorization requirement.

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
