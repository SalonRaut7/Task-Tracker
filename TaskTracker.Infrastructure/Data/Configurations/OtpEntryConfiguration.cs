using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities.Identity;

namespace TaskTracker.Infrastructure.Data.Configurations;

public class OtpEntryConfiguration : IEntityTypeConfiguration<OtpEntry>
{
    public void Configure(EntityTypeBuilder<OtpEntry> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CodeHash).HasMaxLength(256).IsRequired();
        builder.Property(o => o.Purpose).IsRequired();

        builder.HasIndex(o => new { o.UserId, o.Purpose });
    }
}
