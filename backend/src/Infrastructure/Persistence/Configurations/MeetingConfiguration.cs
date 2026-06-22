using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.ToTable("Meetings");
        builder.HasKey(m => m.Id);
        builder.Ignore(m => m.DomainEvents);

        builder.Property(m => m.Address).IsRequired().HasMaxLength(400);
        builder.Property(m => m.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Comment).HasMaxLength(1000);

        builder.HasIndex(m => m.Date);
        builder.HasIndex(m => m.Status);

        builder.HasOne(m => m.SalesRep)
            .WithMany()
            .HasForeignKey(m => m.SalesRepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Contact)
            .WithMany()
            .HasForeignKey(m => m.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        // Company ilişkisi CompanyConfiguration üzerinden tanımlıdır.

        // Görüşme notları: Meeting silinince notlar da silinir.
        builder.HasMany(m => m.Notes)
            .WithOne()
            .HasForeignKey(n => n.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
