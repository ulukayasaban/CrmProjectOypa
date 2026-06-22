using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        builder.Ignore(n => n.DomainEvents);

        builder.Property(n => n.RecipientUserId).IsRequired();

        builder.Property(n => n.Message).IsRequired().HasMaxLength(500);

        builder.Property(n => n.Title).HasMaxLength(200);

        builder.Property(n => n.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(n => n.SenderName).HasMaxLength(150);

        builder.Property(n => n.Link).HasMaxLength(300);

        // Per-alıcı okundu sorgularını hızlandıran bileşik index
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_RecipientUserId_IsRead");

        builder.HasIndex(n => n.CreatedAtUtc);
    }
}
