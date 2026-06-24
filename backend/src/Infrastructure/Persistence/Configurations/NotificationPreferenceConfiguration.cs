using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(p => p.Id);
        builder.Ignore(p => p.DomainEvents);

        builder.Property(p => p.UserId).IsRequired();

        // Enum değerini string olarak sakla; yeni enum değerleri şemayı kırmaz.
        builder.Property(p => p.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(p => p.Enabled).IsRequired();

        // Bir kullanıcı — bir tip kombinasyonu benzersiz olmalı (upsert semantiği için).
        builder.HasIndex(p => new { p.UserId, p.Type })
            .IsUnique()
            .HasDatabaseName("UX_NotificationPreferences_UserId_Type");

        // Kullanıcıya göre tercih listesi çekimi için index.
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_NotificationPreferences_UserId");
    }
}
