using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Infrastructure.Data.Seed;

/// <summary>
/// Seeds default roles and a SuperAdmin user at startup.
/// Idempotent — safe to run on every application start.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AdminSeedOptions adminSeedOptions,
        ILogger logger)
    {
        // ── 1. Seed roles ──────────────────────────────────────
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    Description = $"System role: {roleName}",
                    IsSystemRole = true
                };
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                    logger.LogInformation("Created role: {Role}", roleName);
                else
                    logger.LogWarning("Failed to create role {Role}: {Errors}", roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // ── 2. Seed SuperAdmin user ────────────────────────────
        if (!adminSeedOptions.Enabled)
        {
            logger.LogInformation("SuperAdmin seeding is disabled via configuration.");
            return;
        }

        if (string.IsNullOrWhiteSpace(adminSeedOptions.Email) ||
            string.IsNullOrWhiteSpace(adminSeedOptions.Password))
        {
            logger.LogWarning("SuperAdmin seeding skipped: AdminSeed:Email or AdminSeed:Password is missing.");
            return;
        }

        var adminEmail = adminSeedOptions.Email.Trim();
        var adminPassword = adminSeedOptions.Password;

        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin is null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = string.IsNullOrWhiteSpace(adminSeedOptions.FirstName)
                    ? "Super"
                    : adminSeedOptions.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(adminSeedOptions.LastName)
                    ? "Admin"
                    : adminSeedOptions.LastName.Trim(),
                EmailConfirmed = true,  // pre-verified
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.SuperAdmin);
                logger.LogInformation("SuperAdmin user created: {Email}", adminEmail);
            }
            else
            {
                logger.LogWarning("Failed to create SuperAdmin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
