using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class AbonnementConfiguration : IEntityTypeConfiguration<Abonnement>
{
    public void Configure(EntityTypeBuilder<Abonnement> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.PlanName).HasMaxLength(100);
        builder.Property(a => a.Preis).HasColumnType("decimal(18,2)");

        builder.HasOne(a => a.Mandant)
            .WithMany()
            .HasForeignKey(a => a.MandantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Rechnungen)
            .WithOne(r => r.Abonnement)
            .HasForeignKey(r => r.AbonnementId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
