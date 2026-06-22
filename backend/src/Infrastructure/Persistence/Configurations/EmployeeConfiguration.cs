using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;
using Oypa.Crm.Infrastructure.Identity;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(e => e.Id);
        builder.Ignore(e => e.DomainEvents);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(150);
        builder.Property(e => e.FullName).HasMaxLength(150);
        builder.Property(e => e.Email).HasMaxLength(256);

        // Self-referencing yönetici ilişkisi; silme kısıtlıdır (alt düğüm varken silinemez).
        builder.HasOne(e => e.Manager)
            .WithMany()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ManagerId);

        // Opsiyonel ApplicationUser FK: hesap silinirse employee bağlantısı null olur.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(e => e.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.ApplicationUserId);
    }
}
