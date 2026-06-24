using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

/// <summary>AuditLog tablosu — yalnızca yazılır, güncellenmez/silinmez.</summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        // long PK — büyük hacimlerde int sınırını aşmamak için
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityColumn();

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.UserId)
            .IsRequired(false);

        builder.Property(a => a.UserName)
            .HasMaxLength(256);

        builder.Property(a => a.TimestampUtc)
            .IsRequired();

        builder.Property(a => a.Changes)
            .HasMaxLength(2100);

        // Sık sorgulanan: entityName+entityId ile tarih aralığı filtresi
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.TimestampUtc);
    }
}
