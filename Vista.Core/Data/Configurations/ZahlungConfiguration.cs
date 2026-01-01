using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class ZahlungConfiguration : IEntityTypeConfiguration<Zahlung>
{
    public void Configure(EntityTypeBuilder<Zahlung> builder)
    {
        builder.HasKey(z => z.Id);
        builder.Property(z => z.Betrag).HasColumnType("decimal(18,2)");
        builder.Property(z => z.TransaktionId).HasMaxLength(200);
        builder.Property(z => z.IBAN).HasMaxLength(34);
        builder.Property(z => z.Hinweise).HasMaxLength(500);

        builder.HasOne(z => z.Mandant)
            .WithMany()
            .HasForeignKey(z => z.MandantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
