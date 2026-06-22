namespace Oypa.Crm.Contracts.Goals;

/// <summary>Haftalık snapshot özeti.</summary>
public sealed record GoalWeekDto(
    DateOnly WeekStart,
    int Target,
    int Achieved,
    int Percent);
