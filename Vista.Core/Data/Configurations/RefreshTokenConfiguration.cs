using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Token).IsRequired().HasMaxLength(512);
        builder.HasIndex(r => r.Token).IsUnique();
        builder.Property(r => r.BenutzerId).IsRequired();

        builder.HasOne(r => r.Benutzer)
            .WithMany()
            .HasForeignKey(r => r.BenutzerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
