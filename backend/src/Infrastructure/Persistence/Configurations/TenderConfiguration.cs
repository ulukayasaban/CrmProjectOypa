using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class TenderConfiguration : IEntityTypeConfiguration<Tender>
{
    public void Configure(EntityTypeBuilder<Tender> builder)
    {
        builder.ToTable("Tenders");
        builder.HasKey(t => t.Id);
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.Title).IsRequired().HasMaxLength(250);
        builder.Property(t => t.TenderNumber).HasMaxLength(60);
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.Property(t => t.Sector).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(30);

        builder.Property(t => t.EstimatedValue).HasPrecision(18, 2);
        builder.Property(t => t.Volume).HasPrecision(18, 2);

        builder.HasIndex(t => t.CompanyId);
        builder.HasIndex(t => t.Sector);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.TenderDate);

        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AssignedSalesRep)
            .WithMany()
            .HasForeignKey(t => t.AssignedSalesRepId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
