using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class MailDraftConfiguration : IEntityTypeConfiguration<MailDraft>
{
    public void Configure(EntityTypeBuilder<MailDraft> builder)
    {
        builder.ToTable("MailDrafts");
        builder.HasKey(d => d.Id);
        builder.Ignore(d => d.DomainEvents);

        builder.Property(d => d.To).IsRequired().HasMaxLength(256);
        builder.Property(d => d.Cc).HasMaxLength(256);
        builder.Property(d => d.Subject).IsRequired().HasMaxLength(300);
        builder.Property(d => d.Body).IsRequired();

        builder.HasIndex(d => d.Sent);
    }
}
