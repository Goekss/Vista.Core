using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class AnsprechpartnerConfiguration : IEntityTypeConfiguration<Ansprechpartner>
{
    public void Configure(EntityTypeBuilder<Ansprechpartner> builder)
    {
        builder.HasOne(x => x.Filiale)
            .WithMany(x => x.Ansprechpartner)
            .HasForeignKey(x => x.FilialeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
