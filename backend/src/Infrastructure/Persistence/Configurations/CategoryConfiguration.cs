using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(80);
        builder.Property(c => c.Color).IsRequired().HasMaxLength(7);

        // Kategori adı benzersiz olmalı (soft-delete dikkate alınmaz — veritabanı düzeyinde)
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
