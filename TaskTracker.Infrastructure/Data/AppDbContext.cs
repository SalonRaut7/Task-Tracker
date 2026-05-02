using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Entities.Identity;
using TaskTracker.Domain.Events;

namespace TaskTracker.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    private readonly IPublisher _publisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    // ── Existing ──────────────────────────────────────────────
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // ── Identity extensions ───────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpEntry> OtpEntries => Set<OtpEntry>();

    // ── Domain entities ───────────────────────────────────────
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<UserOrganization> UserOrganizations => Set<UserOrganization>();
    public DbSet<UserProject> UserProjects => Set<UserProject>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // MUST call — configures Identity tables

        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ── Existing TaskItem configuration ───────────────────
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable(tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Tasks_StartDate_EndDate",
                    "\"StartDate\" IS NULL OR \"EndDate\" IS NULL OR \"StartDate\" <= \"EndDate\"");

                tableBuilder.HasCheckConstraint(
                    "CK_Tasks_CreatedAt_UpdatedAt",
                    "\"CreatedAt\" <= \"UpdatedAt\"");
            });

            entity.HasKey(t => t.Id);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditStamps();
        var domainEvents = CollectTaskDomainEvents();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        ClearTaskDomainEvents();
        PublishDomainEventsAsync(domainEvents, CancellationToken.None).GetAwaiter().GetResult();
        return result;
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return SaveChangesAsync(true, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool              acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditStamps();
        return SaveChangesAndPublishAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task<int> SaveChangesAndPublishAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken)
    {
        var domainEvents = CollectTaskDomainEvents();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        ClearTaskDomainEvents();
        await PublishDomainEventsAsync(domainEvents, cancellationToken);
        return result;
    }

    private void ApplyAuditStamps()
    {
        var nowUtc = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                        entry.Property(t => t.CreatedAt).CurrentValue = nowUtc;
                    if (entry.Entity.UpdatedAt == default)
                        entry.Property(t => t.UpdatedAt).CurrentValue = nowUtc;
                    break;

                case EntityState.Modified:
                    entry.Property(t => t.CreatedAt).IsModified = false;
                    entry.Property(t => t.UpdatedAt).IsModified = true;
                    break;
            }
        }
    }

    private List<TaskChangedDomainEvent> CollectTaskDomainEvents()
    {
        var events = new List<TaskChangedDomainEvent>();

        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            if (entry.Entity.DomainEvents.Count == 0)
            {
                continue;
            }

            events.AddRange(entry.Entity.DomainEvents);
        }

        return events;
    }

    private void ClearTaskDomainEvents()
    {
        foreach (var entry in ChangeTracker.Entries<TaskItem>())
        {
            if (entry.Entity.DomainEvents.Count == 0)
            {
                continue;
            }

            entry.Entity.ClearDomainEvents();
        }
    }

    private async Task PublishDomainEventsAsync(
        IReadOnlyList<TaskChangedDomainEvent> domainEvents,
        CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
