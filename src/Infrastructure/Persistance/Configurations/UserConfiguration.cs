using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.UserName).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.PhoneNumber).IsUnique();

        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DateOfBirth).IsRequired(false);
        builder.Property(x => x.Street).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.Street2).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.City).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.State).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.PostalCode).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.CountryId).IsRequired(false);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired(false);
        builder.Property(x => x.LastLoginUtc).IsRequired(false);

        builder.HasOne(x => x.Country)
            .WithMany()
            .HasForeignKey(x => x.CountryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
