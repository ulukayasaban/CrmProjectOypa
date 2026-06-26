using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class CompanyNoteConfiguration : IEntityTypeConfiguration<CompanyNote>
{
    public void Configure(EntityTypeBuilder<CompanyNote> builder)
    {
        builder.ToTable("CompanyNotes");
        builder.HasKey(n => n.Id);
        // GUID her zaman istemci tarafından üretilir; EF Core'a sunucu tarafından üretilmediğini
        // bildiriyoruz. Aksi hâlde navigation collection üzerinden eklenen entity'ler InMemory/SQLite
        // testlerinde yanlışlıkla Modified (güncelleme) olarak işaretlenir.
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Ignore(n => n.DomainEvents);

        builder.Property(n => n.Content).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.AuthorName).IsRequired().HasMaxLength(150);

        builder.HasIndex(n => n.CompanyId);
        builder.HasIndex(n => n.CreatedAtUtc);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(n => n.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
