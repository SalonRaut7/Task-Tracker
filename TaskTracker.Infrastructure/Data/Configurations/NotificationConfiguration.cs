using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.Property(n => n.ActorName).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(500).IsRequired();

        // Indexes for fast querying
        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Notifications_Recipient_CreatedAt");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");

        // Relationships
        builder.HasOne(n => n.Recipient)
            .WithMany()
            .HasForeignKey(n => n.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
