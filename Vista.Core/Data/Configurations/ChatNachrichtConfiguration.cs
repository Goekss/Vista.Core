using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vista.Core.Models;

namespace Vista.Core.Data.Configurations;

public class ChatNachrichtConfiguration : IEntityTypeConfiguration<ChatNachricht>
{
    public void Configure(EntityTypeBuilder<ChatNachricht> builder)
    {
        builder.HasOne(x => x.Absender)
            .WithMany()
            .HasForeignKey(x => x.AbsenderId)
            .HasPrincipalKey(x => x.Id)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
