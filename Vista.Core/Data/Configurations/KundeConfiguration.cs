using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class KundeConfiguration : IEntityTypeConfiguration<Kunde>
{
    public void Configure(EntityTypeBuilder<Kunde> builder)
    {
        builder.HasMany(x => x.Filialen)
            .WithOne(x => x.Kunde)
            .HasForeignKey(x => x.KundeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Ansprechpartner)
            .WithOne(x => x.Kunde)
            .HasForeignKey(x => x.KundeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Projekte)
            .WithOne(x => x.Kunde)
            .HasForeignKey(x => x.KundeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
