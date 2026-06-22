using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class SalesRepConfiguration : IEntityTypeConfiguration<SalesRep>
{
    public void Configure(EntityTypeBuilder<SalesRep> builder)
    {
        builder.ToTable("SalesReps");
        builder.HasKey(r => r.Id);
        builder.Ignore(r => r.DomainEvents);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Email).IsRequired().HasMaxLength(256);

        builder.HasIndex(r => r.Email);
        builder.HasIndex(r => r.EmployeeId);

        // Personel silinirse temsilcinin bağlantısı null olur.
        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
