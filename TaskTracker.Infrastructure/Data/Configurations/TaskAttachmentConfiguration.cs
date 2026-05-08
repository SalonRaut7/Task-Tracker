using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class TaskAttachmentConfiguration : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.UploaderId).IsRequired();
        builder.Property(a => a.CloudinaryPublicId).HasMaxLength(255).IsRequired();
        builder.Property(a => a.CloudinaryUrl).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ResourceType).HasMaxLength(20).IsRequired();

        builder.HasIndex(a => a.TaskId);

        builder.HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Uploader)
            .WithMany()
            .HasForeignKey(a => a.UploaderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
