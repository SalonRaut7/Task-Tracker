namespace TaskTracker.Application.Options;

// Cache TTL configuration. All values are tunable via appsettings.json
// without requiring a redeployment.
public sealed class CacheOptions
{
    public const string SectionName = "CacheSettings";
    // Sliding expiration in minutes for user-status cache entries
    public int UserStatusSlidingMinutes { get; init; } = 5;

    // Absolute expiration in minutes for user-status cache entries.
    public int UserStatusAbsoluteMinutes { get; init; } = 15;

    // Sliding expiration in minutes for IsSuperAdmin cache entries
    public int SuperAdminSlidingMinutes { get; init; } = 10;

    // Absolute expiration in minutes for IsSuperAdmin cache entries
    public int SuperAdminAbsoluteMinutes { get; init; } = 30;

    // Sliding expiration in minutes for user permissions bundle
    public int PermissionsSlidingMinutes { get; init; } = 5;

    // Absolute expiration in minutes for user permissions bundle
    public int PermissionsAbsoluteMinutes { get; init; } = 15;

    // Sliding expiration in minutes for user org/project ID lists
    public int MembershipIdsSlidingMinutes { get; init; } = 5;

    // Absolute expiration in minutes for user org/project ID lists
    public int MembershipIdsAbsoluteMinutes { get; init; } = 15;

    // Sliding expiration in minutes for user role-in-scope lookups
    public int RoleInScopeSlidingMinutes { get; init; } = 5;

    // Absolute expiration in minutes for user role-in-scope lookups
    public int RoleInScopeAbsoluteMinutes { get; init; } = 15;

    // Sliding expiration in minutes for resource-to-project scope mappings
    public int ResourceScopeSlidingMinutes { get; init; } = 30;

    // Absolute expiration in minutes for resource-to-project scope mappings
    public int ResourceScopeAbsoluteMinutes { get; init; } = 120;

    // Sliding expiration in minutes for project metadata
    public int ProjectMetadataSlidingMinutes { get; init; } = 10;

    // Absolute expiration in minutes for project metadata
    public int ProjectMetadataAbsoluteMinutes { get; init; } = 30;

    public TimeSpan UserStatusSliding => TimeSpan.FromMinutes(UserStatusSlidingMinutes);
    public TimeSpan UserStatusAbsolute => TimeSpan.FromMinutes(UserStatusAbsoluteMinutes);
    public TimeSpan SuperAdminSliding => TimeSpan.FromMinutes(SuperAdminSlidingMinutes);
    public TimeSpan SuperAdminAbsolute => TimeSpan.FromMinutes(SuperAdminAbsoluteMinutes);
    public TimeSpan PermissionsSliding => TimeSpan.FromMinutes(PermissionsSlidingMinutes);
    public TimeSpan PermissionsAbsolute => TimeSpan.FromMinutes(PermissionsAbsoluteMinutes);
    public TimeSpan MembershipIdsSliding => TimeSpan.FromMinutes(MembershipIdsSlidingMinutes);
    public TimeSpan MembershipIdsAbsolute => TimeSpan.FromMinutes(MembershipIdsAbsoluteMinutes);
    public TimeSpan RoleInScopeSliding => TimeSpan.FromMinutes(RoleInScopeSlidingMinutes);
    public TimeSpan RoleInScopeAbsolute => TimeSpan.FromMinutes(RoleInScopeAbsoluteMinutes);
    public TimeSpan ResourceScopeSliding => TimeSpan.FromMinutes(ResourceScopeSlidingMinutes);
    public TimeSpan ResourceScopeAbsolute => TimeSpan.FromMinutes(ResourceScopeAbsoluteMinutes);
    public TimeSpan ProjectMetadataSliding => TimeSpan.FromMinutes(ProjectMetadataSlidingMinutes);
    public TimeSpan ProjectMetadataAbsolute => TimeSpan.FromMinutes(ProjectMetadataAbsoluteMinutes);
}
