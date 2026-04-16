using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Key).HasMaxLength(10).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        // Project key must be unique within an organization
        builder.HasIndex(p => new { p.OrganizationId, p.Key }).IsUnique();

        builder.HasMany(p => p.Sprints)
               .WithOne(s => s.Project)
               .HasForeignKey(s => s.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Epics)
               .WithOne(e => e.Project)
               .HasForeignKey(e => e.ProjectId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
