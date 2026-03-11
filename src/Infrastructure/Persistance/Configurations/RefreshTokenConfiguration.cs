using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("UserTokens");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => new { t.UserId, t.Token, t.ExpiresUtc }).IsUnique();

        builder.Property(t => t.Token)
            .IsRequired()
            .HasMaxLength(512);
        builder.Property(t => t.UserId)
            .IsRequired();
        builder.Property(t => t.ExpiresUtc)
            .IsRequired();
        builder.Property(t => t.RevokedAtUtc);
        builder.Property(t => t.IpAddress).HasMaxLength(64);
        builder.Property(t => t.UserAgent).HasMaxLength(512);
        builder.Property(t => t.LastUsedUtc);

        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsActive);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
