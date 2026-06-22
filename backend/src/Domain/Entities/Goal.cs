using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Bir yöneticiye atanmış, ekip bazlı haftalık görüşme hedefi.</summary>
public class Goal : BaseEntity
{
    private Goal() { }

    public Goal(Guid assigneeEmployeeId, GoalSegment segment, int weeklyTarget, string? title)
    {
        if (weeklyTarget <= 0)
            throw new ArgumentException("Haftalık hedef 0'dan büyük olmalıdır.", nameof(weeklyTarget));

        AssigneeEmployeeId = assigneeEmployeeId;
        Segment = segment;
        WeeklyTarget = weeklyTarget;
        Title = title;
        IsActive = true;
    }

    public Guid AssigneeEmployeeId { get; private set; }
    public Employee? AssigneeEmployee { get; private set; }

    public GoalSegment Segment { get; private set; }
    public int WeeklyTarget { get; private set; }
    public string? Title { get; private set; }
    public bool IsActive { get; private set; }

    public void UpdateDetails(GoalSegment segment, int weeklyTarget, string? title)
    {
        if (weeklyTarget <= 0)
            throw new ArgumentException("Haftalık hedef 0'dan büyük olmalıdır.", nameof(weeklyTarget));

        Segment = segment;
        WeeklyTarget = weeklyTarget;
        Title = title;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reassign(Guid employeeId)
    {
        AssigneeEmployeeId = employeeId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
