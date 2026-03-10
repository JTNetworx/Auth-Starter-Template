using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class UserProfileImageConfiguration : IEntityTypeConfiguration<UserProfileImage>
{
    public void Configure(EntityTypeBuilder<UserProfileImage> builder)
    {
        builder.ToTable("UserProfileImages");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .HasMaxLength(450);

        builder.Property(x => x.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Data)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithOne(u => u.ProfileImage)
            .HasForeignKey<UserProfileImage>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
