using Oypa.Crm.Domain.Common;
using Oypa.Crm.Domain.Enums;

namespace Oypa.Crm.Domain.Entities;

/// <summary>
/// Bir yöneticiye atanmış, ekip bazlı haftalık görüşme hedefi.
/// Soft-delete destekler; <see cref="ISoftDelete.MarkDeleted"/> ile mantıksal silme yapılır.
/// </summary>
public class Goal : BaseEntity, ISoftDelete
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

    // ────────── ISoftDelete ──────────

    /// <summary>Hedef silinmiş olarak işaretlenmiş mi.</summary>
    public bool IsDeleted { get; private set; }

    /// <summary>Silme zaman damgası (UTC). Silinmediyse null.</summary>
    public DateTime? DeletedAtUtc { get; private set; }

    /// <summary>Hedefi mantıksal olarak siler (devre dışı bırakır ve gizler).</summary>
    public void MarkDeleted(DateTime utcNow)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Silinmiş hedefi geri yükler.</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
