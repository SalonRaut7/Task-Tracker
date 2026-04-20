using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class UserProjectConfiguration : IEntityTypeConfiguration<UserProject>
{
    public void Configure(EntityTypeBuilder<UserProject> builder)
    {
        builder.HasKey(membership => new { membership.UserId, membership.ProjectId });

        builder.Property(membership => membership.JoinedAt)
            .IsRequired();

        builder.HasOne(membership => membership.User)
            .WithMany(user => user.ProjectMemberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(membership => membership.Project)
            .WithMany(project => project.UserMemberships)
            .HasForeignKey(membership => membership.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}