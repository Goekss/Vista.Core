using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class ProjektConfiguration : IEntityTypeConfiguration<Projekt>
{
    public void Configure(EntityTypeBuilder<Projekt> builder)
    {
        builder.HasMany(x => x.Benutzer)
            .WithMany(x => x.Projekte)
            .UsingEntity<Dictionary<string, object>>(
                "BenutzerProjekt",
                right => right.HasOne<Benutzer>()
                    .WithMany()
                    .HasForeignKey("BenutzerId")
                    .HasPrincipalKey(x => x.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Projekt>()
                    .WithMany()
                    .HasForeignKey("ProjektId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("BenutzerProjekte");
                    join.HasKey("ProjektId", "BenutzerId");
                });
    }
}
