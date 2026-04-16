using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Infrastructure.Data.Seed;

/// <summary>
/// Seeds default roles, role-permission mappings, and a SuperAdmin user at startup.
/// Idempotent — safe to run on every application start.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
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

        // ── 2. Seed role → permission mappings ─────────────────
        foreach (var roleName in AppRoles.All)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var permissions = AppPermissions.GetPermissionsForRole(roleName);
            var existingPermissions = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.Permission)
                .ToListAsync();

            foreach (var permission in permissions)
            {
                if (!existingPermissions.Contains(permission))
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        Permission = permission
                    });
                }
            }
        }
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Role-permission mappings seeded.");

        // ── 3. Seed SuperAdmin user ────────────────────────────
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
