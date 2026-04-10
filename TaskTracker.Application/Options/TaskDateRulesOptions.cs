// TaskTracker.Application/Options/TaskDateRulesOptions.cs
namespace TaskTracker.Application.Options;

public sealed class TaskDateRulesOptions
{
    /// <summary>
    /// The configuration section key that must match appsettings.json exactly.
    /// "TaskDateRules": { ... }
    /// </summary>
    public const string SectionName = "TaskDateRules";

    // Fallback constants — used when config is missing or invalid
    public const int FallbackSprintDays = 10;
    public static readonly int[] FallbackAllowedExtensionDays = [1, 5, 10];

    /// <summary>
    /// Bound from appsettings.json: "DefaultSprintDurationDays": 10
    /// </summary>
    public int DefaultSprintDurationDays { get; init; } = FallbackSprintDays;

    /// <summary>
    /// Bound from appsettings.json: "AllowedEndDateExtensionDays": [1, 5, 10]
    /// </summary>
    public int[] AllowedEndDateExtensionDays { get; init; } = FallbackAllowedExtensionDays;

    // ── Computed — handlers use these directly, zero fallback logic in handlers ──

    public int EffectiveSprintDays =>
        DefaultSprintDurationDays > 0
            ? DefaultSprintDurationDays
            : FallbackSprintDays;

    public IReadOnlySet<int> EffectiveAllowedExtensionDays =>
        (AllowedEndDateExtensionDays is { Length: > 0 }
            ? AllowedEndDateExtensionDays
            : FallbackAllowedExtensionDays)
        .ToHashSet();
}