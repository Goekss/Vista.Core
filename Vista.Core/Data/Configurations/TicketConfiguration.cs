using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasOne(x => x.ZugewiesenAn)
            .WithMany()
            .HasForeignKey(x => x.ZugewiesenAnId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Kunde)
            .WithMany()
            .HasForeignKey(x => x.KundeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Projekt)
            .WithMany()
            .HasForeignKey(x => x.ProjektId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Nachrichten)
            .WithOne(x => x.Ticket)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
