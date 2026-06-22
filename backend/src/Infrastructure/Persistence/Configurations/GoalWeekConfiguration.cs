using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Oypa.Crm.Domain.Entities;

namespace Oypa.Crm.Infrastructure.Persistence.Configurations;

public sealed class GoalWeekConfiguration : IEntityTypeConfiguration<GoalWeek>
{
    public void Configure(EntityTypeBuilder<GoalWeek> builder)
    {
        builder.ToTable("GoalWeeks");
        builder.HasKey(w => w.Id);
        builder.Ignore(w => w.DomainEvents);

        builder.Property(w => w.WeekStart).IsRequired().HasColumnType("date");
        builder.Property(w => w.TargetValue).IsRequired();
        builder.Property(w => w.AchievedCount).IsRequired();

        // (GoalId, WeekStart) çifti benzersiz olmalıdır.
        builder.HasIndex(w => new { w.GoalId, w.WeekStart }).IsUnique();

        builder.HasOne(w => w.Goal)
            .WithMany()
            .HasForeignKey(w => w.GoalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
