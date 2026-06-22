using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class MeetingNoteConfiguration : IEntityTypeConfiguration<MeetingNote>
{
    public void Configure(EntityTypeBuilder<MeetingNote> builder)
    {
        builder.ToTable("MeetingNotes");
        builder.HasKey(n => n.Id);
        // GUID her zaman istemci tarafından üretilir; EF Core'a sunucu tarafından üretilmediğini
        // bildiriyoruz. Aksi hâlde navigation collection üzerinden eklenen entity'ler InMemory/SQLite
        // testlerinde yanlışlıkla Modified (güncelleme) olarak işaretlenir.
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Ignore(n => n.DomainEvents);

        builder.Property(n => n.Content).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.AuthorName).IsRequired().HasMaxLength(150);
        builder.Property(n => n.AuthorTitle).HasMaxLength(256);

        builder.HasIndex(n => n.MeetingId);
        builder.HasIndex(n => n.CreatedAtUtc);
    }
}
