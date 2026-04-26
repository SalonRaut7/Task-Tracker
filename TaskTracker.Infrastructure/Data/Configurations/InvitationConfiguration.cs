using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("Invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ScopeType)
            .IsRequired();

        builder.Property(i => i.ScopeId)
            .IsRequired();

        builder.Property(i => i.InviteeEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(i => i.InviteeUserId)
            .HasMaxLength(450);

        builder.Property(i => i.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(i => i.ExpiresAt)
            .IsRequired();

        builder.Property(i => i.Status)
            .IsRequired();

        builder.Property(i => i.InvitedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        // Index for token-based lookups (accept invite flow)
        builder.HasIndex(i => i.TokenHash)
            .IsUnique();

        // Filtered unique index: only one Pending invite per scope + email
        builder.HasIndex(i => new { i.ScopeType, i.ScopeId, i.InviteeEmail })
            .HasFilter("\"Status\" = 0")
            .IsUnique();

        // Index for listing invites by scope
        builder.HasIndex(i => new { i.ScopeType, i.ScopeId });

        // FK to inviter user
        builder.HasOne(i => i.InvitedByUser)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
