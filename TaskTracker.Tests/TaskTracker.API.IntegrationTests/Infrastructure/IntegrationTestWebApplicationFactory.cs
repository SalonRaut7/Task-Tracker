using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskTracker.API.Controllers;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Constants;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Enums;
using TaskTracker.Infrastructure.Data;
using TaskTracker.Infrastructure.Services;

namespace TaskTracker.Tests.TaskTracker.API.IntegrationTests.Infrastructure;

public sealed class IntegrationTestWebApplicationFactory : WebApplicationFactory<TasksController>, IAsyncLifetime
{
    public const string AdminEmail = "integration.admin@tasktracker.local";
    public const string AdminPassword = "Admin123!Pass";

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "IntegrationSecretKey_AtLeast_32_Chars!",
                ["JwtSettings:Issuer"] = "TaskTracker",
                ["JwtSettings:Audience"] = "TaskTrackerUsers",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["AdminSeed:Enabled"] = "false",
                ["IdentitySettings:LockoutMinutes"] = "5",
                ["IdentitySettings:MaxFailedAccessAttempts"] = "5",
                ["NotificationSettings:DueSoonWindowHours"] = "24",
                ["NotificationSettings:DueDateMonitorIntervalMinutes"] = "60",
                ["NotificationSettings:RetentionCountPerUser"] = "100",
                ["Cloudinary:CloudName"] = "test",
                ["Cloudinary:ApiKey"] = "test",
                ["Cloudinary:ApiSecret"] = "test",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<IFileStorageService>();

            var dueDateMonitorDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(DueDateMonitorService))
                .ToList();

            foreach (var descriptor in dueDateMonitorDescriptors)
            {
                services.Remove(descriptor);
            }

            _connection ??= new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.AddSingleton<IFileStorageService, FakeFileStorageService>();
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        await SeedRolesAndAdminAsync(scope.ServiceProvider);
    }

    public new async Task DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        base.Dispose();
    }

    public async Task<Guid> EnsureProjectAsync(string projectKey = "INT")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingProjectId = await dbContext.Projects
            .Where(p => p.Key == projectKey)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        if (existingProjectId != Guid.Empty)
        {
            return existingProjectId;
        }

        var organization = new Organization
        {
            Name = "Integration Test Org",
            Slug = $"int-org-{Guid.NewGuid():N}",
            Description = "Organization seeded for integration tests."
        };

        var project = new Project
        {
            OrganizationId = organization.Id,
            Name = "Integration Test Project",
            Key = projectKey,
            Description = "Project seeded for integration tests."
        };

        dbContext.Organizations.Add(organization);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync();

        return project.Id;
    }

    public async Task<Guid> GetOrganizationIdForProjectAsync(Guid projectId)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var organizationId = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => project.OrganizationId)
            .FirstOrDefaultAsync();

        if (organizationId == Guid.Empty)
        {
            throw new InvalidOperationException($"Project '{projectId}' was not found.");
        }

        return organizationId;
    }

    public async Task<(int TaskId, Guid ProjectId)> EnsureTaskAsync(Guid projectId, string title = "Integration Task")
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var adminUserId = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Email == AdminEmail)
            .Select(user => user.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            throw new InvalidOperationException($"Admin user '{AdminEmail}' was not found.");
        }

        var task = TaskItem.Create(
            projectId,
            epicId: null,
            sprintId: null,
            assigneeId: null,
            reporterId: adminUserId,
            title: title,
            description: "Task created for integration testing.",
            status: Status.NotStarted,
            priority: TaskPriority.Medium,
            startDate: DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            utcNow: DateTime.UtcNow);

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        return (task.Id, projectId);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        return await CreateAuthenticatedClientAsync(AdminEmail, AdminPassword);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthResponseEnvelope>();
        if (loginPayload is null || string.IsNullOrWhiteSpace(loginPayload.AccessToken))
        {
            throw new InvalidOperationException("Failed to parse access token from login response.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginPayload.AccessToken);

        return client;
    }

    public async Task<HttpClient> CreateProjectMemberClientAsync(Guid projectId, string role)
    {
        var member = await CreateProjectMemberAsync(projectId, role);
        return await CreateAuthenticatedClientAsync(member.Email, member.Password);
    }

    public async Task<(string UserId, string Email, string Password)> CreateProjectMemberAsync(
        Guid projectId,
        string role)
    {
        const string password = "Member123!Pass";
        var email = $"int-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@tasktracker.local";

        using var scope = Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => new { p.Id, p.OrganizationId })
            .FirstOrDefaultAsync();

        if (project is null)
        {
            throw new InvalidOperationException($"Project '{projectId}' was not found.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Integration",
            LastName = role,
            EmailConfirmed = true,
            IsActive = true,
            IsArchived = false,
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Project member creation failed: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
        }

        dbContext.UserOrganizations.Add(new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = project.OrganizationId,
            Role = role,
        });

        dbContext.UserProjects.Add(new UserProject
        {
            UserId = user.Id,
            ProjectId = project.Id,
            Role = role,
        });

        await dbContext.SaveChangesAsync();

        return (user.Id, email, password);
    }

    public async Task<(string UserId, string Email, string Password)> CreateRegisteredUserAsync()
    {
        const string password = "Member123!Pass";
        var email = $"int-user-{Guid.NewGuid():N}@tasktracker.local";

        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Integration",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true,
            IsArchived = false,
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"User creation failed: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
        }

        return (user.Id, email, password);
    }

    private static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var createRoleResult = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role,
                    Description = $"Integration role: {role}",
                    IsSystemRole = true,
                });

                if (!createRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Role seeding failed for '{role}': {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FirstName = "Integration",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true,
                IsArchived = false,
            };

            var createUserResult = await userManager.CreateAsync(adminUser, AdminPassword);
            if (!createUserResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Admin user seeding failed: {string.Join(", ", createUserResult.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, AppRoles.SuperAdmin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(adminUser, AppRoles.SuperAdmin);
            if (!addRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Assigning SuperAdmin role failed: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    private sealed class AuthResponseEnvelope
    {
        public string AccessToken { get; set; } = string.Empty;
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<FileUploadResult> UploadAsync(
            byte[] fileData,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var publicId = $"fake-{Guid.NewGuid():N}";
            var url = $"https://fake-storage.local/{publicId}/{Uri.EscapeDataString(fileName)}";
            return Task.FromResult(new FileUploadResult(publicId, url, "raw"));
        }

        public Task DeleteAsync(
            string publicId,
            string resourceType,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
