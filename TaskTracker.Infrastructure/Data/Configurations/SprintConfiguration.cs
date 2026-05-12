using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Goal).HasMaxLength(1000);

        builder.HasIndex(s => s.ProjectId);
        // Filtered unique index: only one Active sprint (Status = 1) per project.
        // This is the final DB-level guard against the "two active sprints" race condition.
        builder.HasIndex(s => new { s.ProjectId, s.Status })
            .HasFilter("\"Status\" = 1")
            .IsUnique()
            .HasDatabaseName("IX_Sprints_OneActivePerProject");

        builder.ToTable(t =>
        {
            // Prevents invalid enum values outside 0–4 from ever reaching the DB.
            t.HasCheckConstraint("CK_Sprints_ValidStatus",
                "\"Status\" IN (0, 1, 2, 3, 4)");

            // Mirrors the domain invariant at the DB level.
            t.HasCheckConstraint("CK_Sprints_StartDate_EndDate",
                "\"StartDate\" <= \"EndDate\"");

            // Mirrors the audit invariant at the DB level.
            t.HasCheckConstraint("CK_Sprints_CreatedAt_UpdatedAt",
                "\"CreatedAt\" <= \"UpdatedAt\"");
        });
    }
}
