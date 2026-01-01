using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class RechnungConfiguration : IEntityTypeConfiguration<Rechnung>
{
    public void Configure(EntityTypeBuilder<Rechnung> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Nummer).HasMaxLength(50);
        builder.Property(r => r.Betrag).HasColumnType("decimal(18,2)");
        builder.Property(r => r.PdfPfad).HasMaxLength(500);

        builder.HasOne(r => r.Mandant)
            .WithMany()
            .HasForeignKey(r => r.MandantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Zahlungen)
            .WithOne(z => z.Rechnung)
            .HasForeignKey(z => z.RechnungId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
