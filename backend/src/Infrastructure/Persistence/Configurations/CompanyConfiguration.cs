using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);

        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Sector).HasConversion<string>().HasMaxLength(40);
        builder.Property(c => c.Phone).IsRequired().HasMaxLength(30);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Address).IsRequired().HasMaxLength(400);

        // Opsiyonel profil alanları
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Website).HasMaxLength(200);
        builder.Property(c => c.TaxNumber).HasMaxLength(20);
        builder.Property(c => c.Source).HasConversion<string>().HasMaxLength(20);

        builder.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.LeadStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.CustomerStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => c.Email);
        builder.HasIndex(c => c.AssignedSalesRepId);

        builder.HasOne(c => c.AssignedSalesRep)
            .WithMany()
            .HasForeignKey(c => c.AssignedSalesRepId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(c => c.Contacts)
            .WithOne(c => c.Company!)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Meetings)
            .WithOne(m => m.Company!)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
