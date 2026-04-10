using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<TaskItem> Tasks => Set<TaskItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskItem>(entity =>
            {
                // DB-level safety net — catches anything that bypassed domain logic
                entity.ToTable(tableBuilder =>
                {
                    tableBuilder.HasCheckConstraint(
                        "CK_Tasks_StartDate_EndDate",
                        "\"StartDate\" IS NULL OR \"EndDate\" IS NULL OR \"StartDate\" <= \"EndDate\"");

                    tableBuilder.HasCheckConstraint(
                        "CK_Tasks_CreatedAt_UpdatedAt",
                        "\"CreatedAt\" <= \"UpdatedAt\"");
                });

                // Tell EF Core about the private constructor
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
            // Capture once — consistent timestamp across all entities in this save
            var nowUtc = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<TaskItem>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        // Only fill defaults — domain Create() may have already set these
                        if (entry.Entity.CreatedAt == default)
                            entry.Property(t => t.CreatedAt).CurrentValue = nowUtc;

                        if (entry.Entity.UpdatedAt == default)
                            entry.Property(t => t.UpdatedAt).CurrentValue = nowUtc;
                        break;

                    case EntityState.Modified:
                        // Prevent accidental CreatedAt overwrites
                        entry.Property(t => t.CreatedAt).IsModified = false;

                        // Do NOT overwrite UpdatedAt — domain ApplyUpdate() already set it
                        // This avoids two sources of truth for the same timestamp
                        entry.Property(t => t.UpdatedAt).IsModified = true;
                        break;
                }
            }
        }
    }
}