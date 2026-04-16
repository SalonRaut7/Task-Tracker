using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Slug).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(1000);

        builder.HasIndex(o => o.Slug).IsUnique();

        builder.HasMany(o => o.Projects)
               .WithOne(p => p.Organization)
               .HasForeignKey(p => p.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
