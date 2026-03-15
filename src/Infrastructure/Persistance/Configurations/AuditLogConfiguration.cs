using Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.Action).IsRequired().HasMaxLength(100);
        builder.Property(l => l.EntityType).HasMaxLength(100);
        builder.Property(l => l.EntityId).HasMaxLength(450);
        builder.Property(l => l.IpAddress).HasMaxLength(64);
        builder.Property(l => l.UserAgent).HasMaxLength(512);
        builder.Property(l => l.Details); // unbounded JSON

        builder.HasIndex(l => l.Timestamp);
        builder.HasIndex(l => new { l.UserId, l.Timestamp });
        builder.HasIndex(l => l.Action);

        // SetNull — logs persist when a user is deleted (for compliance)
        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
