using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Application.Authorization;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;
using TaskTracker.Infrastructure.Services;
using TaskTracker.Infrastructure.Repositories;

namespace TaskTracker.Infrastructure;

/// Registers all Infrastructure layer services with the DI container.

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Options ──────────────────────────────────────────────
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.Configure<InviteOptions>(configuration.GetSection(InviteOptions.SectionName));
        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.SectionName));
        services.Configure<IdentitySecurityOptions>(configuration.GetSection(IdentitySecurityOptions.SectionName));

        // ── ASP.NET Identity ─────────────────────────────────────
        var identitySecurityOptions = configuration.GetSection(IdentitySecurityOptions.SectionName).Get<IdentitySecurityOptions>() ?? new IdentitySecurityOptions();

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password policy
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // Lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identitySecurityOptions.LockoutMinutes);
            options.Lockout.MaxFailedAccessAttempts = identitySecurityOptions.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = true;

            // User
            options.User.RequireUniqueEmail = true;

            // Sign-in
            options.SignIn.RequireConfirmedEmail = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // ── Services ─────────────────────────────────────────────
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
        services.AddScoped<INotificationPushService, NotificationPushService>();

        // ── Repositories ─────────────────────────────────────────
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IEpicRepository, EpicRepository>();
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IMembershipRepository, MembershipRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
