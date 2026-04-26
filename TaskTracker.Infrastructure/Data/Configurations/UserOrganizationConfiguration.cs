using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class UserOrganizationConfiguration : IEntityTypeConfiguration<UserOrganization>
{
    public void Configure(EntityTypeBuilder<UserOrganization> builder)
    {
        builder.HasKey(membership => new { membership.UserId, membership.OrganizationId });

        builder.Property(membership => membership.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(membership => membership.InvitedByUserId)
            .HasMaxLength(450);

        builder.Property(membership => membership.JoinedAt)
            .IsRequired();

        builder.Property(membership => membership.UpdatedAt)
            .IsRequired();

        builder.HasOne(membership => membership.User)
            .WithMany(user => user.OrganizationMemberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(membership => membership.Organization)
            .WithMany(organization => organization.UserMemberships)
            .HasForeignKey(membership => membership.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(membership => membership.InvitedByUser)
            .WithMany()
            .HasForeignKey(membership => membership.InvitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}