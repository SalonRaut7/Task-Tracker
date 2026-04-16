using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // ── Existing ──────────────────────────────────────────────
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    // ── Identity extensions ───────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpEntry> OtpEntries => Set<OtpEntry>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // ── Domain entities ───────────────────────────────────────
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Epic> Epics => Set<Epic>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<Comment> Comments => Set<Comment>();

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
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyAuditStamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool              acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditStamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
}