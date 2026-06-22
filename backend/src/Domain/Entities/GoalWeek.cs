using Oypa.Crm.Domain.Common;

namespace Oypa.Crm.Domain.Entities;

/// <summary>Belirli bir ISO haftasına ait hedef ilerlemesi (snapshot).</summary>
public class GoalWeek : BaseEntity
{
    private GoalWeek() { }

    public GoalWeek(Guid goalId, DateOnly weekStart, int targetValue)
    {
        GoalId = goalId;
        WeekStart = weekStart;
        TargetValue = targetValue;
        AchievedCount = 0;
    }

    public Guid GoalId { get; private set; }
    public Goal? Goal { get; private set; }

    /// <summary>Haftanın Pazartesi tarihi.</summary>
    public DateOnly WeekStart { get; private set; }

    /// <summary>O haftaya ait dondurulmuş hedef değeri.</summary>
    public int TargetValue { get; private set; }

    /// <summary>O haftada gerçekleşen görüşme sayısı.</summary>
    public int AchievedCount { get; private set; }

    public void SetAchieved(int count)
    {
        AchievedCount = count;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
