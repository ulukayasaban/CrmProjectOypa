using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);
        builder.Ignore(g => g.DomainEvents);

        builder.Property(g => g.Title).HasMaxLength(200);
        builder.Property(g => g.Segment).HasConversion<int>();
        builder.Property(g => g.WeeklyTarget).IsRequired();
        builder.Property(g => g.IsActive).IsRequired();

        builder.HasIndex(g => g.AssigneeEmployeeId);
        builder.HasIndex(g => g.IsActive);

        // Personel silinirse hedef de silinmez; bütünlük kısıtı (Restrict).
        builder.HasOne(g => g.AssigneeEmployee)
            .WithMany()
            .HasForeignKey(g => g.AssigneeEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
